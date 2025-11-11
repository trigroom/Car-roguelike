using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class PerlinNoiseUtil {
    [Header("Noise Settings")]
    private static  float scale = 500f; // Масштаб шума (чем меньше - тем более детальный)

    private static int octaves = 4; // Количество октав (уровней детализации)
    private static  float persistence = 0.5f; // Влияние каждой следующей октавы (0-1)
    private static  float lacunarity = 2f; // Увеличение частоты для каждой октавы
    

    // БАЗОВАЯ ФУНКЦИЯ ШУМА ПЕРЛИНА

    public static float GetPerlinNoise(float x, float y) {
        // Простейший вариант - один слой шума
       
        return Mathf.PerlinNoise(x / scale, y / scale);
     
    }

    // МНОГООКТАВНЫЙ ШУМ ПЕРЛИНА (Fractal Brownian Motion)
    public static float GetFractalNoise(float x, float y) {
        float value = 0f;
        float amplitude = 1f;
        float frequency = 10f;
        float maxValue = 0f;
        for (int i = 0; i < octaves; i++) {
            // Получаем значение шума для текущей октавы
            float noiseValue = Mathf.PerlinNoise(x / scale * frequency, y / scale * frequency);
            value += noiseValue * amplitude;

            // Накапливаем максимальное значение для нормализации
            maxValue += amplitude;

            // Уменьшаем амплитуду и увеличиваем частоту для следующей октавы
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        // Нормализуем результат к диапазону [0, 1]
        return value / maxValue;
    }

    /*  void Update()
      {
          if (visualize)
          {
              VisualizeNoise();
          }
      }

      void VisualizeNoise()
      {
          for (int x = 0; x < width; x++)
          {
              for (int y = 0; y < height; y++)
              {
                  // Получаем значение шума
                  float noiseValue = GetFractalNoise(x, y);

                  // Визуализируем точку (можно заменить на создание объектов)
                  Debug.DrawRay(new Vector3(x, y, 0), Vector3.forward * noiseValue,
                              Color.Lerp(Color.black, Color.white, noiseValue));
              }
          }
      }*/
}
