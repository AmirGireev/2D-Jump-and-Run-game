using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;

public static class OnlineHighscores
{
    static Task initTask;

    // Statics zuruecksetzen
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        initTask = null;
    }

    // Bereit-Status
    static bool Ready
    {
        get
        {
            return UnityServices.State == ServicesInitializationState.Initialized
                && AuthenticationService.Instance.IsSignedIn;
        }
    }

    public static bool IsReady { get { return Ready; } }

    // Eigene ID
    public static string PlayerId
    {
        get { return Ready ? AuthenticationService.Instance.PlayerId : null; }
    }

    // Initialisieren
    public static Task InitAsync()
    {
        if (Ready) return Task.CompletedTask;
        if (initTask == null || initTask.IsCompleted) initTask = DoInit();
        return initTask;
    }

    static async Task DoInit()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                Debug.Log("[Online] Initialisiere Unity Services...");
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("[Online] Melde anonym an...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log("[Online] Angemeldet als " + AuthenticationService.Instance.PlayerId);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Online-Highscores nicht verfuegbar: " + e.Message);
        }
    }

    // Zeit hochladen
    public static async Task SubmitAsync(string boardId, double seconds)
    {
        await InitAsync();
        if (!Ready) return;
        try
        {
            await LeaderboardsService.Instance.AddPlayerScoreAsync(boardId, seconds);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Score-Upload fehlgeschlagen: " + e.Message);
        }
    }

    // Top-Liste
    public static async Task<List<LeaderboardEntry>> TopAsync(string boardId, int limit = 10)
    {
        await InitAsync();
        if (!Ready) return null;
        try
        {
            var page = await LeaderboardsService.Instance.GetScoresAsync(
                boardId, new GetScoresOptions { Offset = 0, Limit = limit });
            return page.Results;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Score-Abruf fehlgeschlagen: " + e.Message);
            return null;
        }
    }

    // Eigener Eintrag
    public static async Task<LeaderboardEntry> MyEntryAsync(string boardId)
    {
        await InitAsync();
        if (!Ready) return null;
        try
        {
            return await LeaderboardsService.Instance.GetPlayerScoreAsync(boardId);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Eigener Platz nicht abrufbar: " + e.Message);
            return null;
        }
    }

    // Name setzen
    public static async Task SetNameAsync(string name)
    {
        await InitAsync();
        if (!Ready) return;

        // Schon gesetzt
        string current = AuthenticationService.Instance.PlayerName;
        if (current != null && current.StartsWith(name + "#")) return;

        try { await AuthenticationService.Instance.UpdatePlayerNameAsync(name); }
        catch (Exception e) { Debug.LogWarning("Name setzen fehlgeschlagen: " + e.Message); }
    }
}
