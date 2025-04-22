// CryptoUtils.cs
using System;
using System.Text;
using System.Collections.Generic; // Для списков

public static class CryptoUtils
{
    private static Random random = new Random();

    // --- Базовые Математические Помощники (с учетом переполнения long) ---

    // Модульное умножение (чтобы избежать переполнения a*b)
    // Используется метод "Русского крестьянского умножения" (бинарный метод)
    public static long ModMul(long a, long b, long m)
    {
        long result = 0;
        a %= m;
        b %= m;
        while (b > 0)
        {
            if ((b & 1) == 1) // Если b нечетное
            {
                result = (result + a);
                if (result >= m) result -= m; // Сложение по модулю вручную
            }
            a = (a << 1); // a = a * 2
            if (a >= m) a -= m; // Умножение на 2 по модулю
            b >>= 1;          // b = b / 2
        }
        return result;
    }


    // Модульное возведение в степень (a^b mod m)
    public static long PowMod(long baseVal, long exponent, long modulus)
    {
        long result = 1;
        baseVal %= modulus;
        while (exponent > 0)
        {
            if ((exponent & 1) == 1) // Если степень нечетная
                result = ModMul(result, baseVal, modulus); // Используем ModMul

            baseVal = ModMul(baseVal, baseVal, modulus); // baseVal = (baseVal * baseVal) % modulus
            exponent >>= 1; // exponent = exponent / 2
        }
        return result;
    }

    // Наибольший Общий Делитель (НОД) - Алгоритм Евклида
    public static long Gcd(long a, long b)
    {
        while (b != 0)
        {
            long temp = b;
            b = a % b;
            a = temp;
        }
        return Math.Abs(a); // НОД обычно положителен
    }

    // Расширенный Алгоритм Евклида - находит x, y такие, что ax + by = gcd(a, b)
    // Возвращает gcd(a, b) и записывает x, y через out параметры
    public static long ExtendedGcd(long a, long b, out long x, out long y)
    {
        if (a == 0)
        {
            x = 0;
            y = 1;
            return b;
        }

        long x1, y1;
        long gcd = ExtendedGcd(b % a, a, out x1, out y1);

        x = y1 - (b / a) * x1;
        y = x1;

        return gcd;
    }

    // Модульный Мультипликативный Обратный - находит x такой, что ax ≡ 1 (mod m)
    // Требует, чтобы gcd(a, m) == 1
    public static long ModInverse(long a, long m)
    {
        long x, y;
        long g = ExtendedGcd(a, m, out x, out y);
        if (g != 1)
            throw new ArgumentException($"Обратный элемент для {a} по модулю {m} не существует (НОД != 1).");

        // Приводим x к положительному значению в диапазоне [0, m-1]
        return (x % m + m) % m;
    }

    // --- Тестирование на Простоту ---

    // Тест 1: Пробные деления (простой, хорош для малых чисел)
    public static bool IsPrimeTrialDivision(long n)
    {
        if (n <= 1) return false;
        if (n <= 3) return true;
        if (n % 2 == 0 || n % 3 == 0) return false;

        // Проверяем делители вида 6k ± 1 до sqrt(n)
        long limit = (long)Math.Sqrt(n);
        for (long i = 5; i <= limit; i += 6)
        {
            if (n % i == 0 || n % (i + 2) == 0)
                return false;
        }
        return true;
    }

    // Тест 2: Тест Ферма на простоту (вероятностный)
    // k - количество итераций (точность растет с k)
    public static bool IsPrimeFermat(long n, int k = 10) // Используем 10 раундов
    {
        if (n <= 1 || n == 4) return false; // Базовые случаи
        if (n <= 3) return true;        // 2 и 3 простые
        if (n % 2 == 0) return false;       // Четные > 2 составные

        // Выполняем тест k раз
        for (int i = 0; i < k; i++)
        {
            // Выбираем случайное 'a' в диапазоне [2, n-2]
            long a;
            // Генерируем случайное long в диапазоне [min, max).
            // Используем NextBytes и BitConverter для совместимости со старыми .NET Framework, если нужно.
            // Для .NET 6+ есть Random.Shared.NextInt64(min, max)
            try
            {
                #if NET6_0_OR_GREATER
                    a = Random.Shared.NextInt64(2, n - 1); // Генерирует в [2, n-2) -> нужно n-1
                #else
                    byte[] buf = new byte[8];
                    random.NextBytes(buf);
                    // Преобразуем в положительное число и масштабируем к диапазону [2, n-2]
                    long longRand = BitConverter.ToInt64(buf, 0);
                    a = 2 + Math.Abs(longRand % (n - 3)); // Преобразуем в диапазон [2, n-2]
                #endif
            }
            catch (ArgumentOutOfRangeException) // Срабатывает если n <= 3
            {
                return IsPrimeTrialDivision(n); // Для очень малых n используем точный тест
            }

            // Проверяем a^(n-1) ≡ 1 (mod n)
            if (PowMod(a, n - 1, n) != 1)
                return false; // n точно составное
        }

        // Если прошло все k тестов, n *вероятно* простое
        // (Может быть числом Кармайкла - составным, но проходящим тест Ферма)
        return true;
    }

    // --- Генерация Простого Числа ---
    // Генерирует *вероятно* простое число в заданном диапазоне
    public static long GenerateProbablePrime(long min = 100, long max = 50000) // Ограничиваем max из-за long
    {
        if (min < 2) min = 2;
        if (max <= min) max = min + 10000; // Гарантируем корректный диапазон
        // Ограничение сверху, чтобы n=p*q не переполнило long
        if (max > 100000) max = 100000;
         if (min >= max) min = max -1; // если max стал слишком маленьким

        while (true)
        {
            // Генерируем случайного кандидата в диапазоне
            long candidate;
            #if NET6_0_OR_GREATER
                candidate = Random.Shared.NextInt64(min, max + 1);
            #else
                byte[] buf = new byte[8];
                random.NextBytes(buf);
                long longRand = BitConverter.ToInt64(buf, 0);
                candidate = min + Math.Abs(longRand % (max - min + 1));
            #endif

            // Ищем ближайшее нечетное число (кроме 2)
            if (candidate % 2 == 0)
            {
                if (candidate + 1 <= max) candidate++;
                else if (candidate - 1 >= min) candidate--;
                else continue; // Если диапазон очень узкий и содержит только четное число
            }
             if (candidate < min) continue; // Если кандидат вышел за нижнюю границу

            // Выполняем тесты на простоту
            // Сначала быстрый тест, потом вероятностный
            if (IsPrimeTrialDivision(candidate) && IsPrimeFermat(candidate, 15)) // Увеличим число раундов Ферма
            {
                // Для учебных целей этого достаточно.
                // В реальной системе нужен Миллер-Рабин.
                return candidate;
            }
        }
    }

    // --- Простая Хеш-Функция (Не криптостойкая!) ---
    // Просто для демонстрации принципа подписи хеша
    public static long SimpleHash(string message, long modulus)
    {
        long hash = 0;
        long p = 31; // Небольшое простое число
        long p_pow = 1;

        foreach (char c in message)
        {
            // Используем код символа (простое сложение по модулю)
            hash = (hash + (long)c * p_pow) % modulus;
            p_pow = (p_pow * p) % modulus; // Обновляем степень p
        }
        // Гарантируем положительный результат (хотя % в C# может вернуть отрицательный)
        return (hash % modulus + modulus) % modulus;
    }


    // --- RSA-подобные Операции (используя long) ---
    // Шифрование/дешифрование ОДНОГО числа (например, кода символа или хеша)

    // "Шифрование" числа m: c = m^e mod n
    public static long EncryptLong(long message, long e, long n)
    {
        if (message >= n)
        {
             // В реальном RSA это недопустимо, нужны схемы дополнения.
             // Здесь просто выдаем ошибку или предупреждение.
             Console.WriteLine($"Предупреждение: Сообщение {message} >= модуль N={n}. Результат может быть некорректным.");
             // Можно либо обрезать message % n, либо выбросить исключение.
             // Для демо обрежем: message %= n;
             // Лучше выбросить исключение, чтобы показать проблему:
              throw new ArgumentException($"Сообщение ({message}) должно быть меньше модуля N ({n})");
        }
        return PowMod(message, e, n);
    }

    // "Дешифрование" числа c: m = c^d mod n
    public static long DecryptLong(long ciphertext, long d, long n)
    {
        return PowMod(ciphertext, d, n);
    }
}