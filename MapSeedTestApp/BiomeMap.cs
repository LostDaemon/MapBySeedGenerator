using System;

namespace MapSeedTestApp;

public enum BiomeType
{
    Ocean,
    Beach,
    Plains,
    Forest,
    Desert,
    Mountains,
    Snow
}

public readonly struct LayerSample
{
    public LayerSample(float height, float water, float moisture, float temperature, BiomeType biome, float vegetation)
    {
        Height = height;
        Water = water;
        Moisture = moisture;
        Temperature = temperature;
        Biome = biome;
        Vegetation = vegetation;
    }

    public float Height { get; }
    public float Water { get; }
    public float Moisture { get; }
    public float Temperature { get; }
    public BiomeType Biome { get; }
    public float Vegetation { get; }
}

public sealed class BiomeMap
{
    private readonly int _seed;
    private readonly float _heightScale;
    private readonly float _moistureScale;
    private readonly float _temperatureScale;
    private readonly float _vegetationScale;

    private readonly float _seaLevel;
    private readonly float _beachWidth;
    private readonly float _mountainStart;
    private readonly float _highMountainStart;

    public BiomeMap(
        int seed,
        float heightScale = 0.008f,
        float moistureScale = 0.012f,
        float temperatureScale = 0.006f,
        float vegetationScale = 0.02f,
        float seaLevel = 0.35f,
        float beachWidth = 0.05f,
        float mountainStart = 0.68f,
        float highMountainStart = 0.80f)
    {
        _seed = seed;
        _heightScale = heightScale;
        _moistureScale = moistureScale;
        _temperatureScale = temperatureScale;
        _vegetationScale = vegetationScale;
        _seaLevel = seaLevel;
        _beachWidth = beachWidth;
        _mountainStart = mountainStart;
        _highMountainStart = highMountainStart;
    }

    public LayerSample Sample(int x, int y)
    {
        var height = GetHeight01(x, y);
        var water = GetWater01(height);
        var moisture = GetMoisture01(x, y, height, water);
        var temperature = GetTemperature01(x, y, height, moisture);
        var biome = GetBiome(height, water, moisture, temperature);
        var vegetation = GetVegetation01(x, y, biome, height, moisture, temperature);
        return new LayerSample(height, water, moisture, temperature, biome, vegetation);
    }

    public float GetHeight01(int x, int y)
    {
        return Fbm(x * _heightScale, y * _heightScale, _seed + 11, 5, 2.0f, 0.5f);
    }

    public float GetWater01(float height01)
    {
        if (height01 >= _seaLevel) return 0.0f;
        return Clamp01((_seaLevel - height01) / 0.20f);
    }

    public float GetMoisture01(int x, int y, float height01, float water01)
    {
        if (water01 > 0.0f) return 1.0f;

        var baseMoisture = Fbm(x * _moistureScale, y * _moistureScale, _seed + 23, 4, 2.0f, 0.5f);
        var coast = 1.0f - SmoothStep(_seaLevel + _beachWidth, _seaLevel + _beachWidth + 0.25f, height01);
        var altitudeDryness = SmoothStep(_seaLevel + 0.10f, 1.0f, height01);

        var moisture = baseMoisture * 0.75f + coast * 0.25f;
        moisture *= 1.0f - altitudeDryness * 0.35f;
        return Clamp01(moisture);
    }

    public float GetTemperature01(int x, int y, float height01, float moisture01)
    {
        var baseTemp = Fbm(x * _temperatureScale, y * _temperatureScale, _seed + 37, 4, 2.0f, 0.5f);
        var altitudeCooling = SmoothStep(_seaLevel, 1.0f, height01);

        var temperature = baseTemp;
        temperature -= altitudeCooling * 0.55f;
        temperature -= moisture01 * 0.15f;
        return Clamp01(temperature);
    }

    public BiomeType GetBiome(float height01, float water01, float moisture01, float temperature01)
    {
        if (water01 > 0.0f) return BiomeType.Ocean;
        if (height01 < _seaLevel + _beachWidth) return BiomeType.Beach;

        if (height01 > _highMountainStart) return temperature01 < 0.45f ? BiomeType.Snow : BiomeType.Mountains;
        if (height01 > _mountainStart) return BiomeType.Mountains;

        if (temperature01 > 0.65f && moisture01 < 0.40f) return BiomeType.Desert;
        if (moisture01 > 0.60f) return BiomeType.Forest;
        return BiomeType.Plains;
    }

    public float GetVegetation01(int x, int y, BiomeType biome, float height01, float moisture01, float temperature01)
    {
        if (biome is BiomeType.Ocean or BiomeType.Beach) return 0.0f;

        var noise = Fbm(x * _vegetationScale, y * _vegetationScale, _seed + 51, 3, 2.0f, 0.5f);
        var moistureFactor = Clamp01((moisture01 - 0.25f) / 0.75f);
        var temperatureFactor = Clamp01((temperature01 - 0.15f) / 0.85f);
        var altitudeFactor = 1.0f - SmoothStep(_mountainStart, 1.0f, height01);

        var biomeFactor = biome switch
        {
            BiomeType.Desert => 0.15f,
            BiomeType.Plains => 0.70f,
            BiomeType.Forest => 1.00f,
            BiomeType.Mountains => 0.35f,
            BiomeType.Snow => 0.10f,
            _ => 0.0f
        };

        var veg = noise;
        veg *= moistureFactor;
        veg *= temperatureFactor;
        veg *= altitudeFactor;
        veg *= biomeFactor;
        return Clamp01(veg);
    }

    public (byte r, byte g, byte b) GetColor(int x, int y)
    {
        var s = Sample(x, y);
        return BiomeToColor(s.Biome);
    }

    public static (byte r, byte g, byte b) HeightToColor(float height01)
    {
        var v = ToByte(height01);
        return (v, v, v);
    }

    public static (byte r, byte g, byte b) WaterToColor(float water01)
    {
        if (water01 <= 0.0f) return (0, 0, 0);
        var b = (byte)(90 + (int)(165.0f * Clamp01(water01)));
        var g = (byte)(25 + (int)(55.0f * Clamp01(water01)));
        return (0, g, b);
    }

    public static (byte r, byte g, byte b) MoistureToColor(float moisture01)
    {
        var v = Clamp01(moisture01);
        var r = (byte)(20 + (int)(20.0f * v));
        var g = (byte)(50 + (int)(140.0f * v));
        var b = (byte)(60 + (int)(195.0f * v));
        return (r, g, b);
    }

    public static (byte r, byte g, byte b) TemperatureToColor(float temperature01)
    {
        var t = Clamp01(temperature01);
        return LerpColor((30, 60, 210), (220, 70, 35), t);
    }

    public static (byte r, byte g, byte b) VegetationToColor(float vegetation01)
    {
        var v = Clamp01(vegetation01);
        var g = (byte)(30 + (int)(225.0f * v));
        return (0, g, 0);
    }

    public static (byte r, byte g, byte b) BiomeToColor(BiomeType biome)
    {
        return biome switch
        {
            BiomeType.Ocean => (0, 70, 160),
            BiomeType.Beach => (235, 220, 170),
            BiomeType.Plains => (80, 170, 60),
            BiomeType.Forest => (25, 110, 35),
            BiomeType.Desert => (210, 190, 120),
            BiomeType.Mountains => (120, 120, 120),
            BiomeType.Snow => (245, 245, 245),
            _ => (255, 0, 255)
        };
    }

    public static (byte r, byte g, byte b) ComposeFinal(BiomeType biome, float height01, float water01, float vegetation01)
    {
        var (r0, g0, b0) = BiomeToColor(biome);
        var shade = 0.65f + 0.35f * Clamp01(height01);

        var r = ClampByte((int)(r0 * shade));
        var g = ClampByte((int)(g0 * shade));
        var b = ClampByte((int)(b0 * shade));

        if (water01 <= 0.0f)
        {
            var veg = Clamp01(vegetation01);
            r = ClampByte(r - (int)(veg * 20.0f));
            g = ClampByte(g + (int)(veg * 90.0f));
            b = ClampByte(b - (int)(veg * 20.0f));
        }

        return ((byte)r, (byte)g, (byte)b);
    }

    private static float Fbm(float x, float y, int seed, int octaves, float lacunarity, float gain)
    {
        var amplitude = 1.0f;
        var frequency = 1.0f;
        var sum = 0.0f;
        var norm = 0.0f;

        for (var i = 0; i < octaves; i++)
        {
            sum += amplitude * SmoothValueNoise(x * frequency, y * frequency, seed + i * 1013);
            norm += amplitude;
            amplitude *= gain;
            frequency *= lacunarity;
        }

        return sum / norm;
    }

    private static float SmoothValueNoise(float x, float y, int seed)
    {
        var x0 = (int)MathF.Floor(x);
        var y0 = (int)MathF.Floor(y);
        var x1 = x0 + 1;
        var y1 = y0 + 1;

        var tx = x - x0;
        var ty = y - y0;

        var v00 = Value01(x0, y0, seed);
        var v10 = Value01(x1, y0, seed);
        var v01 = Value01(x0, y1, seed);
        var v11 = Value01(x1, y1, seed);

        var sx = Fade(tx);
        var sy = Fade(ty);

        var ix0 = Lerp(v00, v10, sx);
        var ix1 = Lerp(v01, v11, sx);
        return Lerp(ix0, ix1, sy);
    }

    private static float Fade(float t) => t * t * (3.0f - 2.0f * t);

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static float Value01(int x, int y, int seed)
    {
        unchecked
        {
            var h = (uint)seed;
            h ^= 0x9E3779B9u;
            h += (uint)x * 0x85EBCA6Bu;
            h = (h ^ (h >> 13)) * 0xC2B2AE35u;
            h += (uint)y * 0x27D4EB2Fu;
            h = (h ^ (h >> 16)) * 0x85EBCA6Bu;
            return (h & 0x00FFFFFFu) / 16777215.0f;
        }
    }

    private static float Clamp01(float v)
    {
        if (v < 0.0f) return 0.0f;
        if (v > 1.0f) return 1.0f;
        return v;
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        var t = Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3.0f - 2.0f * t);
    }

    private static byte ToByte(float v)
    {
        var c = Clamp01(v);
        return (byte)(int)(c * 255.0f);
    }

    private static int ClampByte(int v)
    {
        if (v < 0) return 0;
        if (v > 255) return 255;
        return v;
    }

    private static (byte r, byte g, byte b) LerpColor((byte r, byte g, byte b) a, (byte r, byte g, byte b) b, float t)
    {
        var tt = Clamp01(t);
        var r = ClampByte(a.r + (int)((b.r - a.r) * tt));
        var g = ClampByte(a.g + (int)((b.g - a.g) * tt));
        var bb = ClampByte(a.b + (int)((b.b - a.b) * tt));
        return ((byte)r, (byte)g, (byte)bb);
    }
}
