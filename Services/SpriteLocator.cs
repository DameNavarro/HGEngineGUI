using System;
using System.IO;

namespace HGEngineGUI.Services
{
    public static class SpriteLocator
    {
        // Very first pass: try a few plausible locations. We can expand later.
        // Returns a path or null if not found.
        public static string? FindSmallSpritePath(int speciesId)
        {
            if (ProjectContext.RootPath == null) return null;

            // 1) rawdata/battle_sprite/*.png (common naming often like 7_###.png). Not reliable without a map.
            // 2) data/graphics/sprites/... The repo uses many narcs; for now, fallback icons.

            // Provide a simple built-in placeholder when missing.
            return null;
        }

        public static string? FindIconForSpeciesMacroName(string speciesMacro)
        {
            if (ProjectContext.RootPath == null) return null;
            if (string.IsNullOrWhiteSpace(speciesMacro)) return null;

            // Normalize: SPECIES_BULBASAUR -> bulbasaur
            const string prefix = "SPECIES_";
            string dirName = speciesMacro.StartsWith(prefix, StringComparison.Ordinal)
                ? speciesMacro[prefix.Length..]
                : speciesMacro;
            dirName = dirName.ToLowerInvariant();
            // Some names include double underscores or similar; compact them
            while (dirName.Contains("__")) dirName = dirName.Replace("__", "_");

            string spritesRoot = Path.Combine(ProjectContext.RootPath, "data", "graphics", "sprites");
            string candidateDir = Path.Combine(spritesRoot, dirName);
            if (!Directory.Exists(candidateDir))
                return null;

            string iconPng = Path.Combine(candidateDir, "icon.png");
            if (File.Exists(iconPng)) return iconPng;

            // fallback: male/front.png
            string maleFront = Path.Combine(candidateDir, "male", "front.png");
            if (File.Exists(maleFront)) return maleFront;
            // fallback: female/front.png
            string femaleFront = Path.Combine(candidateDir, "female", "front.png");
            if (File.Exists(femaleFront)) return femaleFront;
            return null;
        }
    }
}


