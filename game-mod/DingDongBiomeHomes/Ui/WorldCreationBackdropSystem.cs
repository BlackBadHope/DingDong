using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.States;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Ui;

// Client decoration only: tasks, weights, random state and world data are unchanged.
public sealed class WorldCreationBackdropSystem : ModSystem
{
    private static readonly Stopwatch Clock = new();
    private static bool failed, loading;
    private static double loadStart, lastEtaSample = -1, eta = -1, etaGeneration = -1;
    public override void Load()
    {
        if (Main.dedServ) return;
        failed = loading = false; Clock.Restart(); WorldChronicleSkin.Load(Mod);
        On_UIWorldLoad.Draw += DrawGeneration;
        On_GenPass.Apply += ObservePass;
        On_Main.DrawMenu += DrawLocalLoading;
    }
    public override void Unload()
    {
        if (!Main.dedServ) { On_UIWorldLoad.Draw -= DrawGeneration; On_GenPass.Apply -= ObservePass; On_Main.DrawMenu -= DrawLocalLoading; }
        WorldChronicleSkin.Unload(); WorldChronicleStages.Reset(); Clock.Stop();
    }
    private static void ObservePass(On_GenPass.orig_Apply orig, GenPass self, GenerationProgress progress, GameConfiguration configuration)
    {
        // Observing a pass must not block orig or swallow its generation exception.
        try { WorldChronicleStages.Begin(self, Clock.Elapsed.TotalSeconds); } catch (Exception ex) { Disable(ex); }
        orig(self, progress, configuration);
    }
    private static void DrawGeneration(On_UIWorldLoad.orig_Draw orig, UIWorldLoad self, SpriteBatch batch)
    {
        Rectangle screen = self.GetDimensions().ToRectangle();
        if (failed || !WorldChronicleSkin.Ready || screen.Width < 800 || screen.Height < 500) { orig(self, batch); return; }
        try { DrawGenerationBook(batch, screen); return; }
        catch (Exception ex) { Disable(ex); }
        orig(self, batch);
    }
    private static void DrawGenerationBook(SpriteBatch batch, Rectangle screen)
    {
        double now = Clock.Elapsed.TotalSeconds;
        WorldChronicleSkin.Draw(batch, screen, now);
        Rectangle b = WorldChronicleSkin.BookBounds(screen), page = WorldChronicleSkin.RightPage(b);
        float u = b.Width / 1500f;
        GenerationProgress progress = WorldGenerator.CurrentGenerationProgress;
        WorldChronicleStages.Snapshot stage = WorldChronicleStages.Current;
        float total = MathHelper.Clamp((float)(progress?.TotalProgress ?? 0), 0, 1);
        float local = MathHelper.Clamp((float)(progress?.Value ?? 0), 0, 1);
        double elapsed = stage == null ? 0 : Math.Max(0, now - stage.GenerationStart);
        if (stage != null && stage.GenerationStart != etaGeneration) { etaGeneration = stage.GenerationStart; eta = -1; lastEtaSample = -1; }
        // ETA sampling is time based, independent of FPS.
        if (elapsed - lastEtaSample >= 1 && elapsed >= 15 && total > .02f && total < 1)
        {
            double sample = elapsed * (1 - total) / total;
            eta = eta < 0 ? sample : eta * .85 + sample * .15; lastEtaSample = elapsed;
        }
        WorldChronicleSkin.Heading(batch, WorldChronicleSkin.Local("CreateTitle"), At(page, u, 0, 0, page.Width / u, 46), .92f * u);
        FitText(batch, Main.worldName, page, 54 * u, 1.02f * u, WorldChronicleSkin.Muted);
        string counter = stage == null ? WorldChronicleSkin.Local("Preparing") : string.Format(WorldChronicleSkin.Local("StageCounter"), stage.Index + 1, stage.Passes.Length);
        WorldChronicleSkin.Text(batch, counter, new(page.X, page.Y + 96 * u), .86f * u, WorldChronicleSkin.Muted);
        string current = stage == null ? WorldChronicleSkin.Local("Preparing") : WorldChronicleStages.Title(stage.Passes[stage.Index]);
        WorldChronicleSkin.Wrapped(batch, current, At(page, u, 0, 122, page.Width / u, 48), 1.06f * u);
        string message = progress?.Message;
        if (string.IsNullOrWhiteSpace(message)) message = WorldChronicleStages.Explanation(stage == null ? null : stage.Passes[stage.Index]);
        WorldChronicleSkin.Wrapped(batch, message, At(page, u, 0, 174, page.Width / u, 48), .82f * u, WorldChronicleSkin.Muted);
        WorldChronicleSkin.Progress(batch, At(page, u, 0, 230, page.Width / u, 22), total);
        WorldChronicleSkin.Text(batch, string.Format(WorldChronicleSkin.Local("Overall"), total * 100), new(page.X, page.Y + 260 * u), .88f * u);
        FitText(batch, string.Format(WorldChronicleSkin.Local("Elapsed"), FormatTime(elapsed)), page, 288 * u, .78f * u, WorldChronicleSkin.Muted);
        string remaining = eta >= 0 && total < 1 ? string.Format(WorldChronicleSkin.Local("Remaining"), FormatTime(Math.Round(eta / 10) * 10)) : total >= 1 ? WorldChronicleSkin.Local("Finishing") : WorldChronicleSkin.Local("Estimating");
        FitText(batch, remaining, page, 311 * u, .78f * u, WorldChronicleSkin.Muted);
        WorldChronicleSkin.Progress(batch, At(page, u, 0, 342, page.Width / u, 10), local);
        WorldChronicleSkin.Text(batch, string.Format(WorldChronicleSkin.Local("PassProgress"), local * 100), new(page.X, page.Y + 358 * u), .75f * u, WorldChronicleSkin.Muted);
        // A moving window into ALL enabled passes. No invented macro-stage progress.
        if (stage != null)
        {
            int visible = Math.Clamp((int)((page.Height - 565 * u) / (24 * u)), 3, 7);
            int first = Math.Clamp(stage.Index - visible / 2, 0, Math.Max(0, stage.Passes.Length - visible));
            for (int row = 0; row < visible && first + row < stage.Passes.Length; row++)
            {
                int index = first + row;
                string label = $"{index + 1:00}  {WorldChronicleStages.Title(stage.Passes[index])}";
                if (index == stage.Index) DingDongUiAssets.DrawBookCard(batch, At(page, u, -4, 402 + row * 24, page.Width / u + 8, 24), Color.White);
                Color color = index == stage.Index ? WorldChronicleSkin.Ink : index < stage.Index ? WorldChronicleSkin.Green : WorldChronicleSkin.Muted;
                FitText(batch, label, page, (402 + row * 24) * u, .76f * u, color);
            }
        }
    }
    private static void DrawLocalLoading(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
    {
        orig(self, gameTime);
        bool active = Main.gameMenu && Main.menuMode == 10 && Main.netMode == 0;
        if (!active) { loading = false; return; }
        if (failed || !WorldChronicleSkin.Ready) return;
        double now = Clock.Elapsed.TotalSeconds;
        if (!loading) { loading = true; loadStart = now; }
        var screen = new Rectangle(0, 0, (int)(Main.screenWidth / Main.UIScale), (int)(Main.screenHeight / Main.UIScale));
        if (screen.Width < 800 || screen.Height < 500) return;
        SpriteBatch batch = Main.spriteBatch; bool begun = false;
        try
        {
            // DrawMenu ends its batch. MP connection menus have Cancel and are excluded.
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix); begun = true;
            WorldChronicleSkin.Draw(batch, screen, now, loading: true);
            Rectangle b = WorldChronicleSkin.BookBounds(screen), page = WorldChronicleSkin.RightPage(b); float u = b.Width / 1500f;
            WorldChronicleSkin.Heading(batch, WorldChronicleSkin.Local("LoadTitle"), At(page, u, 0, 0, page.Width / u, 50), .92f * u);
            FitText(batch, Main.ActiveWorldFileData?.Name ?? Main.worldName, page, 64 * u, 1.02f * u, WorldChronicleSkin.Muted);
            WorldChronicleSkin.Wrapped(batch, Main.statusText, At(page, u, 0, 160, page.Width / u, 110), 1.02f * u);
            FitText(batch, string.Format(WorldChronicleSkin.Local("Elapsed"), FormatTime(now - loadStart)), page, 292 * u, .84f * u, WorldChronicleSkin.Muted);
            WorldChronicleSkin.Wrapped(batch, WorldChronicleSkin.Local("LoadNote"), At(page, u, 0, 340, page.Width / u, 96), .84f * u, WorldChronicleSkin.Muted);
            // Saved-world loading exposes no trustworthy total. Do not invent a % or ETA.
        }
        catch (Exception ex) { Disable(ex); }
        finally { if (begun) batch.End(); }
    }
    private static Rectangle At(Rectangle p, float u, float x, float y, float w, float h) => new(p.X + (int)(x * u), p.Y + (int)(y * u), (int)(w * u), (int)(h * u));
    private static void FitText(SpriteBatch batch, string text, Rectangle page, float y, float scale, Color color)
    {
        text ??= ""; var font = FontAssets.MouseText.Value;
        while (text.Length > 1 && font.MeasureString(text).X * scale > page.Width) text = text[..^1];
        WorldChronicleSkin.Text(batch, text, new(page.X, page.Y + y), scale, color);
    }
    private static string FormatTime(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) seconds = 0;
        TimeSpan t = TimeSpan.FromSeconds(Math.Min(seconds, 864000));
        return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"mm\:ss");
    }
    private static void Disable(Exception ex)
    {
        if (!failed) DingDongBiomeHomes.Instance?.Logger.Warn("DINGDONG_WORLD_CHRONICLE|verdict=FALLBACK|reason=" + ex.Message);
        failed = true;
    }
}

internal static class WorldChronicleStages
{
    private static readonly FieldInfo GeneratorField = typeof(WorldGen).GetField("_generator", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo PassesField = typeof(WorldGenerator).GetField("_passes", BindingFlags.Instance | BindingFlags.NonPublic);
    private static object generator;
    private static GenPass[] enabled = Array.Empty<GenPass>();
    private static string[] names = Array.Empty<string>();
    private static double started;
    internal static volatile Snapshot Current;
    internal sealed class Snapshot
    {
        internal readonly string[] Passes; internal readonly int Index; internal readonly double GenerationStart;
        internal Snapshot(string[] passes, int index, double start) { Passes = passes; Index = index; GenerationStart = start; }
    }
    internal static void Reset() { generator = null; enabled = Array.Empty<GenPass>(); names = Array.Empty<string>(); Current = null; }
    internal static void Begin(GenPass pass, double now)
    {
        object actual = GeneratorField?.GetValue(null); if (actual == null) return;
        if (!ReferenceEquals(actual, generator))
        {
            var list = new List<GenPass>(); if (PassesField?.GetValue(actual) is not IList passes) return;
            foreach (GenPass item in passes) if (item.Enabled) list.Add(item);
            enabled = list.ToArray(); names = Array.ConvertAll(enabled, p => p.Name); generator = actual; started = now;
        }
        int index = Array.IndexOf(enabled, pass);
        // Ignore recursively applied children of wrapped compatibility passes.
        if (index >= 0) Current = new Snapshot(names, index, started);
    }
    internal static string Title(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return WorldChronicleSkin.Local("Preparing");
        string key = raw.Replace(" ", "_").Replace(":", "_").Replace("/", "_").Replace("-", "_");
        string value = WorldChronicleSkin.Local("Passes." + key);
        return value.StartsWith("Mods.DingDongBiomeHomes.", StringComparison.Ordinal) ? raw : value;
    }
    internal static string Explanation(string raw)
    {
        string s = (raw ?? "").ToLowerInvariant();
        string key = s.Contains("residen") || s.Contains("settlement") ? "Residences" : s.Contains("ocean") || s.Contains("beach") ? "Ocean" :
            s.Contains("cav") || s.Contains("tunnel") ? "Caves" : s.Contains("ore") || s.Contains("resource") ? "Resources" :
            s.Contains("water") || s.Contains("liquid") || s.Contains("hydrolog") ? "Waters" : s.Contains("terrain") || s == "reset" ? "Terrain" :
            s.Contains("dungeon") || s.Contains("temple") || s.Contains("structure") ? "Structures" : s.Contains("biome") || s.Contains("jungle") || s.Contains("snow") ? "Biomes" :
            s.Contains("decorat") || s.Contains("plant") || s.Contains("grass") ? "Decoration" : s.Contains("receipt") || s.Contains("final") || s.Contains("cleanup") ? "Finalize" : "Generic";
        return WorldChronicleSkin.Local("Help." + key);
    }
}
