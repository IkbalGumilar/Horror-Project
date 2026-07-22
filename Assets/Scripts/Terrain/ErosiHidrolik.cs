using UnityEngine;

public sealed class ErosiHidrolik
{
    private const float GayaGravitasi = 9.81f;
    private const float KecepatanAwal = 1f;
    private const float AirAwal = 1f;
    private const float BatasAirMinimum = 0.001f;
    private const float BatasArahMinimum = 0.0001f;

    private readonly PengaturanErosiHidrolik parameter;
    private readonly System.Random acak;
    private readonly Vector2Int[] offsetSikat;
    private readonly float[] bobotSikat;

    public ErosiHidrolik(PengaturanErosiHidrolik parameter, System.Random acak)
    {
        this.parameter = parameter;
        this.acak = acak;
        (offsetSikat, bobotSikat) = BangunSikatErosi(parameter.radiusKikis);
    }

    public void Terapkan(float[,] tinggi, int resolusi)
    {
        for (int tetes = 0; tetes < parameter.jumlahTetes; tetes++)
        {
            SimulasikanSatuTetes(tinggi, resolusi);
        }
    }

    private void SimulasikanSatuTetes(float[,] tinggi, int resolusi)
    {
        float posisiX = (float)acak.NextDouble() * (resolusi - 1);
        float posisiZ = (float)acak.NextDouble() * (resolusi - 1);
        float arahX = 0f;
        float arahZ = 0f;
        float kecepatan = KecepatanAwal;
        float air = AirAwal;
        float sedimen = 0f;

        for (int langkah = 0; langkah < parameter.umurMaksimumTetes; langkah++)
        {
            int selX = Mathf.Clamp((int)posisiX, 0, resolusi - 2);
            int selZ = Mathf.Clamp((int)posisiZ, 0, resolusi - 2);
            float offsetX = posisiX - selX;
            float offsetZ = posisiZ - selZ;

            Vector2 gradien = HitungGradien(tinggi, selX, selZ, offsetX, offsetZ);
            float tinggiLama = InterpolasiTinggi(tinggi, selX, selZ, offsetX, offsetZ);

            arahX = arahX * parameter.inersia - gradien.x * (1f - parameter.inersia);
            arahZ = arahZ * parameter.inersia - gradien.y * (1f - parameter.inersia);

            float panjangArah = Mathf.Sqrt(arahX * arahX + arahZ * arahZ);
            if (panjangArah < BatasArahMinimum)
            {
                break;
            }

            arahX /= panjangArah;
            arahZ /= panjangArah;
            posisiX += arahX;
            posisiZ += arahZ;

            if (posisiX < 0f || posisiX >= resolusi - 1 || posisiZ < 0f || posisiZ >= resolusi - 1)
            {
                break;
            }

            int selXBaru = Mathf.Clamp((int)posisiX, 0, resolusi - 2);
            int selZBaru = Mathf.Clamp((int)posisiZ, 0, resolusi - 2);
            float offsetXBaru = posisiX - selXBaru;
            float offsetZBaru = posisiZ - selZBaru;
            float tinggiBaru = InterpolasiTinggi(tinggi, selXBaru, selZBaru, offsetXBaru, offsetZBaru);
            float perubahanTinggi = tinggiBaru - tinggiLama;

            float kapasitas = Mathf.Max(-perubahanTinggi, 0.01f) * kecepatan * air * parameter.kapasitasSedimen;

            if (perubahanTinggi > 0f || sedimen > kapasitas)
            {
                float jumlahEndap = perubahanTinggi > 0f
                    ? Mathf.Min(perubahanTinggi, sedimen)
                    : (sedimen - kapasitas) * parameter.lajuPengendapan;
                sedimen -= jumlahEndap;
                EndapkanTinggi(tinggi, selX, selZ, offsetX, offsetZ, jumlahEndap);
            }
            else
            {
                float jumlahKikis = Mathf.Min((kapasitas - sedimen) * parameter.lajuErosi, -perubahanTinggi);
                KikisTinggi(tinggi, resolusi, selX, selZ, jumlahKikis);
                sedimen += jumlahKikis;
            }

            kecepatan = Mathf.Sqrt(Mathf.Max(0f, kecepatan * kecepatan + perubahanTinggi * -GayaGravitasi));
            air *= 1f - parameter.penguapan;

            if (air < BatasAirMinimum)
            {
                break;
            }
        }
    }

    private static float InterpolasiTinggi(float[,] tinggi, int selX, int selZ, float offsetX, float offsetZ)
    {
        float kiriAtas = tinggi[selZ, selX];
        float kananAtas = tinggi[selZ, selX + 1];
        float kiriBawah = tinggi[selZ + 1, selX];
        float kananBawah = tinggi[selZ + 1, selX + 1];

        return Mathf.Lerp(
            Mathf.Lerp(kiriAtas, kananAtas, offsetX),
            Mathf.Lerp(kiriBawah, kananBawah, offsetX),
            offsetZ);
    }

    private static Vector2 HitungGradien(float[,] tinggi, int selX, int selZ, float offsetX, float offsetZ)
    {
        float kiriAtas = tinggi[selZ, selX];
        float kananAtas = tinggi[selZ, selX + 1];
        float kiriBawah = tinggi[selZ + 1, selX];
        float kananBawah = tinggi[selZ + 1, selX + 1];

        float gradienX = (kananAtas - kiriAtas) * (1f - offsetZ) + (kananBawah - kiriBawah) * offsetZ;
        float gradienZ = (kiriBawah - kiriAtas) * (1f - offsetX) + (kananBawah - kananAtas) * offsetX;

        return new Vector2(gradienX, gradienZ);
    }

    private static void EndapkanTinggi(float[,] tinggi, int selX, int selZ, float offsetX, float offsetZ, float jumlah)
    {
        tinggi[selZ, selX] += jumlah * (1f - offsetX) * (1f - offsetZ);
        tinggi[selZ, selX + 1] += jumlah * offsetX * (1f - offsetZ);
        tinggi[selZ + 1, selX] += jumlah * (1f - offsetX) * offsetZ;
        tinggi[selZ + 1, selX + 1] += jumlah * offsetX * offsetZ;
    }

    private void KikisTinggi(float[,] tinggi, int resolusi, int selX, int selZ, float jumlah)
    {
        for (int indeks = 0; indeks < offsetSikat.Length; indeks++)
        {
            int targetX = selX + offsetSikat[indeks].x;
            int targetZ = selZ + offsetSikat[indeks].y;
            if (targetX < 0 || targetX >= resolusi || targetZ < 0 || targetZ >= resolusi)
            {
                continue;
            }

            tinggi[targetZ, targetX] -= jumlah * bobotSikat[indeks];
            if (tinggi[targetZ, targetX] < 0f)
            {
                tinggi[targetZ, targetX] = 0f;
            }
        }
    }

    private static (Vector2Int[], float[]) BangunSikatErosi(int radius)
    {
        System.Collections.Generic.List<Vector2Int> offset = new System.Collections.Generic.List<Vector2Int>();
        System.Collections.Generic.List<float> bobotMentah = new System.Collections.Generic.List<float>();
        float totalBobot = 0f;

        for (int z = -radius; z <= radius; z++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                float jarak = Mathf.Sqrt(x * x + z * z);
                if (jarak > radius)
                {
                    continue;
                }

                float bobot = radius - jarak;
                offset.Add(new Vector2Int(x, z));
                bobotMentah.Add(bobot);
                totalBobot += bobot;
            }
        }

        float[] bobotTernormalisasi = new float[bobotMentah.Count];
        for (int i = 0; i < bobotMentah.Count; i++)
        {
            bobotTernormalisasi[i] = totalBobot > 0f ? bobotMentah[i] / totalBobot : 0f;
        }

        return (offset.ToArray(), bobotTernormalisasi);
    }
}
