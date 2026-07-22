using System.Collections.Generic;
using UnityEngine;

public sealed class PewarnaOtomatis
{
    private readonly LapisanPewarnaan[] lapisan;
    private readonly float lebarPembauran;

    public PewarnaOtomatis(LapisanPewarnaan[] lapisan, float lebarPembauran)
    {
        this.lapisan = lapisan;
        this.lebarPembauran = lebarPembauran;
    }

    public void Terapkan(TerrainData data)
    {
        List<TerrainLayer> terrainLayerValid = new List<TerrainLayer>();
        List<LapisanPewarnaan> lapisanValid = new List<LapisanPewarnaan>();
        for (int i = 0; i < lapisan.Length; i++)
        {
            if (lapisan[i].terrainLayer == null)
            {
                continue;
            }

            terrainLayerValid.Add(lapisan[i].terrainLayer);
            lapisanValid.Add(lapisan[i]);
        }

        if (terrainLayerValid.Count == 0)
        {
            return;
        }

        data.terrainLayers = terrainLayerValid.ToArray();

        int resolusi = data.alphamapResolution;
        float[,,] peta = new float[resolusi, resolusi, terrainLayerValid.Count];

        for (int baris = 0; baris < resolusi; baris++)
        {
            float normalisasiZ = baris / (float)(resolusi - 1);
            for (int kolom = 0; kolom < resolusi; kolom++)
            {
                float normalisasiX = kolom / (float)(resolusi - 1);
                float tinggiMeter = data.GetInterpolatedHeight(normalisasiX, normalisasiZ);
                float kemiringanDerajat = data.GetSteepness(normalisasiX, normalisasiZ);

                float[] bobot = HitungBobotLapisan(lapisanValid, tinggiMeter, kemiringanDerajat);
                for (int indeks = 0; indeks < bobot.Length; indeks++)
                {
                    peta[baris, kolom, indeks] = bobot[indeks];
                }
            }
        }

        data.SetAlphamaps(0, 0, peta);
    }

    private float[] HitungBobotLapisan(List<LapisanPewarnaan> lapisanValid, float tinggiMeter, float kemiringanDerajat)
    {
        float[] bobot = new float[lapisanValid.Count];
        float total = 0f;

        for (int i = 0; i < lapisanValid.Count; i++)
        {
            float bobotTinggi = HitungBobotRentang(tinggiMeter, lapisanValid[i].rentangKetinggianMeter);
            float bobotKemiringan = HitungBobotRentang(kemiringanDerajat, lapisanValid[i].rentangKemiringanDerajat);
            bobot[i] = bobotTinggi * bobotKemiringan * lapisanValid[i].prioritas;
            total += bobot[i];
        }

        if (total <= 0.0001f)
        {
            bobot[0] = 1f;
            return bobot;
        }

        for (int i = 0; i < bobot.Length; i++)
        {
            bobot[i] /= total;
        }

        return bobot;
    }

    private float HitungBobotRentang(float nilai, Vector2 rentang)
    {
        float lebarRentang = Mathf.Max(0.0001f, rentang.y - rentang.x);
        float lebarBaur = lebarRentang * lebarPembauran;

        float masukBawah = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(rentang.x - lebarBaur, rentang.x + lebarBaur, nilai));
        float keluarAtas = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(rentang.y - lebarBaur, rentang.y + lebarBaur, nilai));

        return Mathf.Clamp01(Mathf.Min(masukBawah, keluarAtas));
    }
}
