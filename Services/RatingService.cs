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
                .ThenInclude(bg => bg.BoardGameEloMethods)
                    .ThenInclude(bem => bem.FkBgdEloMethodNavigation)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match == null || match.FkBgdBoardGameNavigation == null) return;

        var activeMethodMapping = match.FkBgdBoardGameNavigation.BoardGameEloMethods
            .FirstOrDefault(em => !em.Inactive);

        var methodEntry = activeMethodMapping?.FkBgdEloMethodNavigation;

        if (methodEntry == null) return;

        string methodName = methodEntry.MethodName;

        var teamGroups = match.BoardGameMatchPlayers
            .GroupBy(mp => mp.BoardGameMatchPlayerResults.FirstOrDefault()?.FinalTeam ?? 0)
            .OrderBy(g => g.Key)
            .ToList();

        var teamsForCalc = new List<List<PlayerRatingState>>();

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

                        // Removed ?? because these fields are likely non-nullable in your Model classes
                        FkBgdBoardGame = match.FkBgdBoardGame,
                        RatingMu = methodEntry.InitialMu,
                        RatingSigma = methodEntry.InitialSigma,

                        MatchesPlayed = 0,
                        TimeCreated = DateTime.UtcNow,
                        CreatedBy = "System",
                        ModifiedBy = "System"
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
            if (currentTeam.Any()) teamsForCalc.Add(currentTeam);
        }

        var ranks = teamsForCalc.Select(t => t.First().ResultRecord.Win ? 1 : 2).ToList();

        switch (methodName)
        {
            case "TrueSkill":
                SimpleRatingCalculator.UpdateTrueSkill(teamsForCalc, ranks);
                break;
            case "Elo":
                if (teamsForCalc.Count == 2 && teamsForCalc[0].Count == 1 && teamsForCalc[1].Count == 1)
                {
                    SimpleRatingCalculator.UpdateElo(teamsForCalc[0][0], teamsForCalc[1][0], ranks[0], ranks[1]);
                }
                break;
            case "Placement":
                SimpleRatingCalculator.UpdatePlacement(teamsForCalc, ranks);
                break;
            default:
                SimpleRatingCalculator.UpdateTrueSkill(teamsForCalc, ranks);
                break;
        }

        foreach (var team in teamsForCalc)
        {
            foreach (var player in team)
            {
                var res = player.ResultRecord;
                var ratingRecord = player.DbRecord;

                res.PreMatchRatingMu = ratingRecord.RatingMu;
                res.PreMatchRatingSigma = ratingRecord.RatingSigma;
                res.RatingChangeMu = (decimal)player.Mu - ratingRecord.RatingMu;
                res.RatingChangeSigma = (decimal)player.Sigma - ratingRecord.RatingSigma;

                ratingRecord.RatingMu = (decimal)player.Mu;
                ratingRecord.RatingSigma = (decimal)player.Sigma;
                ratingRecord.MatchesPlayed++;
                ratingRecord.TimeModified = DateTime.UtcNow;
                ratingRecord.ModifiedBy = "System";
            }
        }

        await _db.SaveChangesAsync();
    }

    private class PlayerRatingState
    {
        public double Mu { get; set; }
        public double Sigma { get; set; }
        public PlayerBoardGameRating DbRecord { get; set; } = null!;
        public BoardGameMatchPlayerResult ResultRecord { get; set; } = null!;
    }

    private static class SimpleRatingCalculator
    {
        private const double Beta = 4.166666;
        private const double Tau = 0.083333;

        public static void UpdateTrueSkill(List<List<PlayerRatingState>> teams, List<int> ranks)
        {
            foreach (var team in teams)
                foreach (var p in team)
                    p.Sigma = Math.Sqrt(p.Sigma * p.Sigma + Tau * Tau);

            if (teams.Count == 2)
            {
                UpdateTwoTeamsTrueSkill(teams[0], teams[1], ranks[0], ranks[1]);
            }
        }

        private static void UpdateTwoTeamsTrueSkill(List<PlayerRatingState> team1, List<PlayerRatingState> team2, int rank1, int rank2)
        {
            double mu1 = team1.Sum(p => p.Mu);
            double sigma1Sq = team1.Sum(p => p.Sigma * p.Sigma);
            double mu2 = team2.Sum(p => p.Mu);
            double sigma2Sq = team2.Sum(p => p.Sigma * p.Sigma);

            double c = Math.Sqrt(2 * Beta * Beta + sigma1Sq + sigma2Sq);
            double diff = mu1 - mu2;
            double v = V(diff / c);
            double shiftMagnitude = (v / c) * (25.0 / 6.0);
            bool team1Won = rank1 < rank2;

            foreach (var p in team1)
            {
                double delta = (p.Sigma * p.Sigma / c) * shiftMagnitude;
                p.Mu += team1Won ? delta : -delta;
                p.Sigma *= 0.99;
            }
            foreach (var p in team2)
            {
                double delta = (p.Sigma * p.Sigma / c) * shiftMagnitude;
                p.Mu += !team1Won ? delta : -delta;
                p.Sigma *= 0.99;
            }
        }

        public static void UpdateElo(PlayerRatingState p1, PlayerRatingState p2, int rank1, int rank2)
        {
            const double K = 32.0;
            double r1 = Math.Pow(10, p1.Mu / 400.0);
            double r2 = Math.Pow(10, p2.Mu / 400.0);

            double expected1 = r1 / (r1 + r2);
            double expected2 = r2 / (r1 + r2);

            double actual1 = rank1 < rank2 ? 1.0 : 0.0;
            double actual2 = 1.0 - actual1;

            p1.Mu += K * (actual1 - expected1);
            p2.Mu += K * (actual2 - expected2);
            p1.Sigma = 0;
            p2.Sigma = 0;
        }

        public static void UpdatePlacement(List<List<PlayerRatingState>> teams, List<int> ranks)
        {
            foreach (var team in teams)
            {
                foreach (var p in team)
                {
                    double currentMatchRank = p.ResultRecord.Win ? 1.0 : 2.0;
                    int totalMatches = p.DbRecord.MatchesPlayed + 1;
                    p.Mu = ((p.Mu * p.DbRecord.MatchesPlayed) + currentMatchRank) / totalMatches;
                }
            }
        }

        private static double V(double t)
        {
            double pdf = Math.Exp(-0.5 * t * t) / Math.Sqrt(2 * Math.PI);
            double cdf = 0.5 * (1.0 + Erf(t / Math.Sqrt(2.0)));
            return pdf / cdf;
        }

        private static double Erf(double x)
        {
            double a1 = 0.254829592, a2 = -0.284496736, a3 = 1.421413741, a4 = -1.453152027, a5 = 1.061405429, p = 0.3275911;
            int sign = (x < 0) ? -1 : 1;
            x = Math.Abs(x);
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);
            return sign * y;
        }
    }
}