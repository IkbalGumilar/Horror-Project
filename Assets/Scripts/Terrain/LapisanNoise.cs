using UnityEngine;

public sealed class LapisanNoise
{
    private readonly ParameterLapisanNoise parameter;
    private readonly Vector2[] offsetOktaf;
    private readonly float sinRotasi;
    private readonly float cosRotasi;

    public LapisanNoise(ParameterLapisanNoise parameter, System.Random acak)
    {
        this.parameter = parameter;
        offsetOktaf = new Vector2[parameter.oktaf];
        for (int oktaf = 0; oktaf < parameter.oktaf; oktaf++)
        {
            offsetOktaf[oktaf] = new Vector2(
                (float)acak.NextDouble() * 10000f,
                (float)acak.NextDouble() * 10000f);
        }

        float radian = parameter.rotasiDerajat * Mathf.Deg2Rad;
        sinRotasi = Mathf.Sin(radian);
        cosRotasi = Mathf.Cos(radian);
    }

    public float Sampel(float x, float z)
    {
        float xRotasi = x * cosRotasi - z * sinRotasi;
        float zRotasi = x * sinRotasi + z * cosRotasi;

        float frekuensi = parameter.frekuensi;
        float amplitudo = 1f;
        float jumlah = 0f;
        float jumlahAmplitudo = 0f;

        for (int oktaf = 0; oktaf < parameter.oktaf; oktaf++)
        {
            float sampelX = xRotasi * frekuensi + offsetOktaf[oktaf].x;
            float sampelZ = zRotasi * frekuensi + offsetOktaf[oktaf].y;
            float nilai = Mathf.PerlinNoise(sampelX, sampelZ);

            if (parameter.jenis == JenisLapisanNoise.Ridge)
            {
                nilai = Mathf.Pow(1f - Mathf.Abs(nilai * 2f - 1f), parameter.pangkat);
            }

            jumlah += nilai * amplitudo;
            jumlahAmplitudo += amplitudo;
            amplitudo *= parameter.persistence;
            frekuensi *= parameter.lacunarity;
        }

        return jumlahAmplitudo > 0f ? jumlah / jumlahAmplitudo : 0f;
    }
}
