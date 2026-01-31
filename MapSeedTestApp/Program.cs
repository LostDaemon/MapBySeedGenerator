using System;
using System.IO;
using System.Security.Cryptography;

using MapSeedTestApp;

const int width = 1024;
const int height = 1024;

int seed;
if (args.Length > 0 && int.TryParse(args[0], out var parsedSeed))
{
    seed = parsedSeed;
}
else
{
    seed = PromptSeed();
}

var outputDir = Path.Combine(AppContext.BaseDirectory, "img");
Directory.CreateDirectory(outputDir);

var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
Console.WriteLine($"seed={seed}");
var paths = RenderLayers(outputDir, timestamp, seed, width, height);
foreach (var p in paths)
{
    Console.WriteLine(p);
}

static int PromptSeed()
{
    while (true)
    {
        Console.Write("Введите seed (Enter = случайный): ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            return RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
        }

        if (int.TryParse(input, out var seed))
        {
            return seed;
        }

        Console.WriteLine("Некорректный seed. Введите целое число или нажмите Enter для случайного.");
    }
}

static string[] RenderLayers(string outputDir, string timestamp, int seed, int width, int height)
{
    var heightPath = BuildPath(outputDir, "layer_height", timestamp, seed);
    var waterPath = BuildPath(outputDir, "layer_water", timestamp, seed);
    var moisturePath = BuildPath(outputDir, "layer_moisture", timestamp, seed);
    var temperaturePath = BuildPath(outputDir, "layer_temperature", timestamp, seed);
    var biomePath = BuildPath(outputDir, "layer_biome", timestamp, seed);
    var vegetationPath = BuildPath(outputDir, "layer_vegetation", timestamp, seed);
    var finalPath = BuildPath(outputDir, "final", timestamp, seed);

    var map = new BiomeMap(seed);

    using var heightBmp = new Bmp24FileWriter(heightPath, width, height);
    using var waterBmp = new Bmp24FileWriter(waterPath, width, height);
    using var moistureBmp = new Bmp24FileWriter(moisturePath, width, height);
    using var temperatureBmp = new Bmp24FileWriter(temperaturePath, width, height);
    using var biomeBmp = new Bmp24FileWriter(biomePath, width, height);
    using var vegetationBmp = new Bmp24FileWriter(vegetationPath, width, height);
    using var finalBmp = new Bmp24FileWriter(finalPath, width, height);

    for (var y = 0; y < height; y++)
    {
        for (var x = 0; x < width; x++)
        {
            var s = map.Sample(x, y);

            var (hr, hg, hb) = BiomeMap.HeightToColor(s.Height);
            heightBmp.SetPixel(x, hr, hg, hb);

            var (wr, wg, wb) = BiomeMap.WaterToColor(s.Water);
            waterBmp.SetPixel(x, wr, wg, wb);

            var (mr, mg, mb) = BiomeMap.MoistureToColor(s.Moisture);
            moistureBmp.SetPixel(x, mr, mg, mb);

            var (tr, tg, tb) = BiomeMap.TemperatureToColor(s.Temperature);
            temperatureBmp.SetPixel(x, tr, tg, tb);

            var (br, bg, bb) = BiomeMap.BiomeToColor(s.Biome);
            biomeBmp.SetPixel(x, br, bg, bb);

            var (vr, vg, vb) = BiomeMap.VegetationToColor(s.Vegetation);
            vegetationBmp.SetPixel(x, vr, vg, vb);

            var (fr, fg, fb) = BiomeMap.ComposeFinal(s.Biome, s.Height, s.Water, s.Vegetation);
            finalBmp.SetPixel(x, fr, fg, fb);
        }

        heightBmp.WriteRow();
        waterBmp.WriteRow();
        moistureBmp.WriteRow();
        temperatureBmp.WriteRow();
        biomeBmp.WriteRow();
        vegetationBmp.WriteRow();
        finalBmp.WriteRow();
    }

    return new[] { heightPath, waterPath, moisturePath, temperaturePath, biomePath, vegetationPath, finalPath };
}

static string BuildPath(string outputDir, string prefix, string timestamp, int seed)
{
    var fileName = $"{prefix}_{timestamp}_seed_{seed}.bmp";
    return Path.Combine(outputDir, fileName);
}
