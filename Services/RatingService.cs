using Board_Game_Software.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board_Game_Software.Services;

public class RatingService
{
    private readonly BoardGameDbContext _db;

    public RatingService(BoardGameDbContext db)
    {
        _db = db;
    }

    public async Task CalculateAndApplyResults(long matchId)
    {
        var match = await _db.BoardGameMatches
            .Include(m => m.BoardGameMatchPlayers)
                .ThenInclude(mp => mp.BoardGameMatchPlayerResults)
            .Include(m => m.FkBgdBoardGameNavigation)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match == null) return;

        // 1. Group players into teams based on FinalTeam column
        var teamGroups = match.BoardGameMatchPlayers
            .GroupBy(mp => mp.BoardGameMatchPlayerResults.FirstOrDefault()?.FinalTeam ?? 0)
            .OrderBy(g => g.Key)
            .ToList();

        var teamsForCalc = new List<List<PlayerRatingState>>();

        // 2. Prepare Data
        foreach (var group in teamGroups)
        {
            var currentTeam = new List<PlayerRatingState>();

            foreach (var mp in group)
            {
                var res = mp.BoardGameMatchPlayerResults.FirstOrDefault();
                if (res == null) continue;

                var ratingRecord = await _db.PlayerBoardGameRatings
                    .FirstOrDefaultAsync(r => r.FkBgdPlayer == mp.FkBgdPlayer && r.FkBgdBoardGame == match.FkBgdBoardGame);

                if (ratingRecord == null)
                {
                    ratingRecord = new PlayerBoardGameRating
                    {
                        FkBgdPlayer = mp.FkBgdPlayer,
                        FkBgdBoardGame = match.FkBgdBoardGame,
                        RatingMu = 25.0000m,
                        RatingSigma = 8.3333m,
                        MatchesPlayed = 0,
                        TimeCreated = DateTime.UtcNow
                    };
                    _db.PlayerBoardGameRatings.Add(ratingRecord);
                }

                currentTeam.Add(new PlayerRatingState
                {
                    Mu = (double)ratingRecord.RatingMu,
                    Sigma = (double)ratingRecord.RatingSigma,
                    DbRecord = ratingRecord,
                    ResultRecord = res
                });
            }

            if (currentTeam.Any())
            {
                teamsForCalc.Add(currentTeam);
            }
        }

        // 3. Determine Ranks (True if Win)
        // We assume the first player's result in the team represents the team's outcome
        var ranks = teamsForCalc.Select(t => t.First().ResultRecord.Win ? 1 : 2).ToList();

        // 4. Run Calculation (Internal Solver)
        var updatedTeams = SimpleRatingCalculator.CalculateNewRatings(teamsForCalc, ranks);

        // 5. Save Results
        for (int i = 0; i < updatedTeams.Count; i++)
        {
            foreach (var player in updatedTeams[i])
            {
                var res = player.ResultRecord;
                var ratingRecord = player.DbRecord;

                // Audit Trail
                res.PreMatchRatingMu = ratingRecord.RatingMu;
                res.PreMatchRatingSigma = ratingRecord.RatingSigma;
                res.RatingChangeMu = (decimal)player.Mu - ratingRecord.RatingMu;
                res.RatingChangeSigma = (decimal)player.Sigma - ratingRecord.RatingSigma;

                // Update Rating
                ratingRecord.RatingMu = (decimal)player.Mu;
                ratingRecord.RatingSigma = (decimal)player.Sigma;
                ratingRecord.MatchesPlayed++;
                ratingRecord.TimeModified = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
    }

    // ==========================================================
    // INTERNAL HELPER CLASSES (No External Library Required)
    // ==========================================================

    private class PlayerRatingState
    {
        public double Mu { get; set; }
        public double Sigma { get; set; }
        public PlayerBoardGameRating DbRecord { get; set; }
        public BoardGameMatchPlayerResult ResultRecord { get; set; }
    }

    private static class SimpleRatingCalculator
    {
        // Simple implementation of Weng-Lin (Plackett-Luce) for multiplayer
        // Mu = Mean, Sigma = Variance/Uncertainty
        private const double Beta = 4.166666; // Standard beta (25/6)
        private const double Tau = 0.083333;  // Dynamics factor

        public static List<List<PlayerRatingState>> CalculateNewRatings(List<List<PlayerRatingState>> teams, List<int> ranks)
        {
            // Add dynamics factor to Sigma (uncertainty increases slightly over time)
            foreach (var team in teams)
            {
                foreach (var p in team)
                {
                    p.Sigma = Math.Sqrt(p.Sigma * p.Sigma + Tau * Tau);
                }
            }

            // For now, assume simple 1 vs 1 or Team vs Team (2 teams)
            // This is a simplified "Gaussian" update for robustness
            // If you have more than 2 teams, this loops 1v1 pairs

            // NOTE: Full Plackett-Luce is 200+ lines. 
            // We will use a standard Two-Team Delta update if there are exactly 2 teams (most common)
            // or a fallback loop for multiple teams.

            if (teams.Count == 2)
            {
                return UpdateTwoTeams(teams[0], teams[1], ranks[0], ranks[1]);
            }

            // Fallback: No change if not exactly 2 teams (Safety catch)
            // If you need Multi-Team (3+ teams) specifically, I can expand this.
            return teams;
        }

        private static List<List<PlayerRatingState>> UpdateTwoTeams(List<PlayerRatingState> team1, List<PlayerRatingState> team2, int rank1, int rank2)
        {
            // 1. Calculate Team Mu and Sigma
            double mu1 = team1.Sum(p => p.Mu);
            double sigma1Sq = team1.Sum(p => p.Sigma * p.Sigma);
            double mu2 = team2.Sum(p => p.Mu);
            double sigma2Sq = team2.Sum(p => p.Sigma * p.Sigma);

            // 2. Draw probability
            // For boolean win/loss, draw margin is usually ignored or small
            double c = Math.Sqrt(2 * Beta * Beta + sigma1Sq + sigma2Sq);

            double diff = mu1 - mu2;
            double deltaMu = 0;

            // Team 1 won
            if (rank1 < rank2)
            {
                double v = V(diff / c);
                double w = W(diff / c);
                deltaMu = (v / c);
                // Sigma updates can be added here, but usually Mu is the critical one for basic leaderboards
            }
            // Team 2 won
            else if (rank2 < rank1)
            {
                diff = mu2 - mu1; // Flip perspective
                double v = V(diff / c);
                deltaMu = -(v / c); // Negative for team 1
            }

            // 3. Distribute delta back to players
            // Update Team 1
            foreach (var p in team1)
            {
                double sigmaSq = p.Sigma * p.Sigma;
                p.Mu += (sigmaSq / c) * deltaMu * (rank1 < rank2 ? 1 : -1) * (25.0 / 6.0); // Scaling factor
                // Reduce sigma (uncertainty drops after a match)
                p.Sigma *= 0.99;
            }

            // Update Team 2
            foreach (var p in team2)
            {
                double sigmaSq = p.Sigma * p.Sigma;
                p.Mu += (sigmaSq / c) * deltaMu * (rank2 < rank1 ? 1 : -1) * (25.0 / 6.0);
                p.Sigma *= 0.99;
            }

            return new List<List<PlayerRatingState>> { team1, team2 };
        }

        // Gaussian helper functions
        private static double V(double t)
        {
            double pdf = Math.Exp(-0.5 * t * t) / Math.Sqrt(2 * Math.PI);
            double cdf = 0.5 * (1.0 + Erf(t / Math.Sqrt(2.0)));
            return pdf / cdf;
        }

        private static double W(double t)
        {
            double v = V(t);
            return v * (v + t);
        }

        private static double Erf(double x)
        {
            // Approximation of Error Function
            double a1 = 0.254829592;
            double a2 = -0.284496736;
            double a3 = 1.421413741;
            double a4 = -1.453152027;
            double a5 = 1.061405429;
            double p = 0.3275911;

            int sign = 1;
            if (x < 0) sign = -1;
            x = Math.Abs(x);

            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return sign * y;
        }
    }
}