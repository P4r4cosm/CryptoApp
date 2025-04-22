// MainWindow.axaml.cs

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading; // <<< Добавьте это для Dispatcher
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks; // <<< Добавьте это для Task

namespace CryptoAppDemoSimple // Убедитесь, что пространство имен совпадает
{
    public partial class MainWindow : Window
    {
        // ... (остальные переменные остаются как есть) ...
        private long p = 0, q = 0;
        private long senderN = 0, senderE = 0, senderD = 0;
        private long receiverN = 0, receiverE = 0, receiverD = 0;
        private List<long> encryptedMessageNumeric = new List<long>();
        private long signatureNumeric = 0;


        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            // this.AttachDevTools();
#endif
            Log("Приложение запущено. Ожидание действий пользователя.");
            // Убедимся что кнопки доступны при старте (если вдруг где-то остались выключенными)
            EnableAllActionButtons(true);
        }

        // --- Вспомогательный метод для логирования (остается как был, с '!')---
        private void Log(string message)
        {
            if (LogTextBox == null || LogScrollViewer == null)
            {
                Console.WriteLine($"[UI НЕ ГОТОВ] {message}");
                return;
            }
            // Используем Dispatcher.UIThread.Post для безопасного обновления UI из любого потока
             Dispatcher.UIThread.Post(() => {
                 LogTextBox.Text += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
                 LogScrollViewer!.ScrollToEnd();
                 if (!string.IsNullOrEmpty(message))
                 {
                     string statusPart = message.Split('.')[0];
                 }
             });
        }

        // --- Вспомогательные методы для очистки полей UI (остаются как были) ---
        private void ClearPrimeUI()
        {
             Dispatcher.UIThread.Post(() => { // Обновление UI в UI потоке
                PTextBox.Text = "";
                QTextBox.Text = "";
             });
            p = 0;
            q = 0;
        }
        private void ClearKeysUI()
        {
             Dispatcher.UIThread.Post(() => {
                SenderNTextBox.Text = SenderETextBox.Text = SenderDTextBox.Text = "";
                ReceiverNTextBox.Text = ReceiverETextBox.Text = ReceiverDTextBox.Text = "";
             });
            senderN = senderE = senderD = 0;
            receiverN = receiverE = receiverD = 0;
        }
        private void ClearEncryptionUI()
        {
             Dispatcher.UIThread.Post(() => {
                EncryptedDataTextBox.Text = "";
                SignatureTextBox.Text = "";
             });
            encryptedMessageNumeric.Clear();
            signatureNumeric = 0;
        }
        private void ClearDecryptionUI()
        {
             Dispatcher.UIThread.Post(() => {
                DecryptedMessageTextBox.Text = "";
                VerificationStatusTextBlock.Text = "Статус проверки...";
             });
        }

        // --- Вспомогательный метод для вкл/выкл кнопок ---
         private void EnableAllActionButtons(bool isEnabled)
         {
              Dispatcher.UIThread.Post(() => { // Обновление UI в UI потоке
                 // Проверяем на null перед доступом
                 if(GeneratePrimesButton != null) GeneratePrimesButton.IsEnabled = isEnabled;
                 if(TestPrimalityButton != null) TestPrimalityButton.IsEnabled = isEnabled;
                 if(GenerateKeysButton != null) GenerateKeysButton.IsEnabled = isEnabled;
                 if(EncryptSignButton != null) EncryptSignButton.IsEnabled = isEnabled;
                 if(DecryptVerifyButton != null) DecryptVerifyButton.IsEnabled = isEnabled;
             });
         }


        // --- Обработчики событий кнопок ---

        // 1. Генерация Простых Чисел (!!! ИЗМЕНЕНА НА ASYNC VOID !!!)
        private async void GeneratePrimes_Click(object sender, RoutedEventArgs e)
        {
            Log("Начало генерации простых чисел p и q (в фоновом режиме)...");
            EnableAllActionButtons(false); // Выключаем кнопки на время операции
            ClearPrimeUI(); // Очищаем старые значения сразу
            ClearKeysUI();
            ClearEncryptionUI();
            ClearDecryptionUI();
            Dispatcher.UIThread.Post(() => {});


            try
            {
                // Выполняем долгую задачу в фоновом потоке с помощью Task.Run
                var result = await Task.Run(() =>
                {
                    long generatedP = 0;
                    long generatedQ = 0;
                    int attempts = 0;
                    const int maxAttempts = 100; // Ограничим попытки найти разные числа

                    // Диапазон для генерации (можно настроить)
                    // Уменьшим для ускорения, т.к. long ограничен и очень большие p,q не нужны
                    long minP = 1000;
                    long maxP = 50000;
                    long minQ = 50001;
                    long maxQ = 100000;

                    do
                    {
                        generatedP = CryptoUtils.GenerateProbablePrime(minP, maxP);
                        generatedQ = CryptoUtils.GenerateProbablePrime(minQ, maxQ);
                        attempts++;
                        if (attempts > maxAttempts)
                        {
                            throw new TimeoutException("Не удалось сгенерировать различные простые числа p и q за заданное число попыток.");
                        }
                    } while (generatedP == generatedQ); // Повторяем, пока p и q не станут разными

                    return (P: generatedP, Q: generatedQ); // Возвращаем результат как кортеж
                });

                // Код ниже выполняется ПОСЛЕ завершения Task.Run, снова в потоке UI

                // Обновляем внутреннее состояние
                p = result.P;
                q = result.Q;

                // Обновляем UI через Dispatcher (хотя после await мы обычно в UI потоке, но так надежнее)
                Dispatcher.UIThread.Post(() =>
                {
                    PTextBox.Text = p.ToString();
                    QTextBox.Text = q.ToString();
                   
                });

                Log($"Успешно сгенерированы: p = {p}, q = {q}");
            }
            catch (Exception ex)
            {
                // Ловим ошибки, возникшие в Task.Run или при обновлении UI
                Log($"ОШИБКА при генерации простых чисел: {ex.Message}");
                 Dispatcher.UIThread.Post(() => {
                     
                 });
                ClearPrimeUI(); // Сбрасываем p и q в UI
            }
            finally
            {
                // Этот блок выполняется всегда, независимо от успеха или ошибки
                EnableAllActionButtons(true); // Включаем кнопки обратно
            }
        }

        // 2. Тест на Простоту (можно оставить синхронным, если он быстрый, или сделать асинхронным по аналогии)
        private void TestPrimality_Click(object sender, RoutedEventArgs e)
        {
            // Этот код может остаться синхронным, если проверка одного числа происходит быстро
            // Если он тоже может быть долгим, переделайте по аналогии с GeneratePrimes_Click
            Log($"Начало проверки числа '{PrimeCandidateInput.Text}' на простоту...");
            if (long.TryParse(PrimeCandidateInput.Text, out long candidate))
            {
                 // ... (остальной код проверки как был) ...
                 if (candidate < 0) candidate = Math.Abs(candidate); // Работаем с положительными

                 Log($"Тестируем число: {candidate}");

                 // Тест 1: Пробные деления
                 bool isPrimeTD = CryptoUtils.IsPrimeTrialDivision(candidate);
                 Log($" -> Тест 1 (Пробные деления): {(isPrimeTD ? "ПРОСТОЕ" : "СОСТАВНОЕ")}");

                 // Тест 2: Ферма
                 bool isPrimeFermat = CryptoUtils.IsPrimeFermat(candidate, 15); // 15 раундов
                 Log($" -> Тест 2 (Ферма, вероятностный, 15 раундов): {(isPrimeFermat ? "Вероятно ПРОСТОЕ" : "СОСТАВНОЕ")}");
                 // ... (остальные комментарии про Ферма) ...
            }
            else
            {
                Log($"Ошибка: Введенное значение '{PrimeCandidateInput.Text}' не является корректным целым числом.");
            }
        }

        // 3. Генерация Ключей (!!! ПЕРЕДЕЛАТЬ НА ASYNC !!!)
         private async void GenerateKeys_Click(object sender, RoutedEventArgs e)
         {
             Log("Начало генерации пар ключей (в фоновом режиме)...");
             if (p == 0 || q == 0)
             {
                 Log("Ошибка: Сначала необходимо сгенерировать простые числа p и q.");
                 return;
             }

             EnableAllActionButtons(false); // Выключаем кнопки
             ClearKeysUI(); // Очищаем старые ключи
             ClearEncryptionUI();
             ClearDecryptionUI();
              Dispatcher.UIThread.Post(() => { });


             try
             {
                 // Выполняем генерацию ключей в фоновом потоке
                 var keyData = await Task.Run(() =>
                 {
                     // Генерируем ключи для Отправителя
                     (long sN, long sE, long sD) = GenerateKeyPairInternal(p, q);

                     // Генерируем ключи для Получателя (используя НОВЫЕ простые числа)
                     long p_rec, q_rec;
                     int attempts = 0;
                     const int maxAttempts = 100;
                     do {
                          // Используем те же диапазоны, что и при генерации p, q
                          p_rec = CryptoUtils.GenerateProbablePrime(1000, 50000);
                          q_rec = CryptoUtils.GenerateProbablePrime(50001, 100000);
                          attempts++;
                          if(attempts > maxAttempts) throw new TimeoutException("Не удалось сгенерировать различные простые числа для получателя.");
                     } while (p_rec == q_rec || p_rec == p || p_rec == q || q_rec == p || q_rec == q);

                     (long rN, long rE, long rD) = GenerateKeyPairInternal(p_rec, q_rec);

                      // Возвращаем все сгенерированные данные
                      return new {
                          SenderN = sN, SenderE = sE, SenderD = sD,
                          ReceiverN = rN, ReceiverE = rE, ReceiverD = rD,
                          PRec = p_rec, QRec = q_rec // Для логирования
                      };
                 });

                  // Код после await Task.Run (в UI потоке)

                  // Обновляем внутреннее состояние
                  senderN = keyData.SenderN; senderE = keyData.SenderE; senderD = keyData.SenderD;
                  receiverN = keyData.ReceiverN; receiverE = keyData.ReceiverE; receiverD = keyData.ReceiverD;

                 // Обновляем UI
                 Dispatcher.UIThread.Post(() => {
                     SenderNTextBox.Text = senderN.ToString();
                     SenderETextBox.Text = senderE.ToString();
                     SenderDTextBox.Text = senderD.ToString();
                     ReceiverNTextBox.Text = receiverN.ToString();
                     ReceiverETextBox.Text = receiverE.ToString();
                     ReceiverDTextBox.Text = receiverD.ToString();
                    
                 });


                 Log($"Ключи Отправителя: N={senderN}, e={senderE}, d={senderD}");
                 Log($"Использованы простые для Получателя: p'={keyData.PRec}, q'={keyData.QRec}");
                 Log($"Ключи Получателя: N={receiverN}, e={receiverE}, d={receiverD}");
                 Log("--- Имитация Распространения Ключей ---");
                 Log($"Отправитель публикует: (N={senderN}, e={senderE})");
                 Log($"Получатель публикует: (N={receiverN}, e={receiverE})");
                 Log("--------------------------------------");

             }
             catch (OverflowException ex)
             {
                  Log($"ОШИБКА (Переполнение): {ex.Message}. Попробуйте сгенерировать p и q меньшего размера.");
                  ClearKeysUI();
                  ClearPrimeUI();
                   Dispatcher.UIThread.Post(() => {});
             }
             catch (ArgumentException ex)
             {
                  Log($"ОШИБКА (Неверный аргумент): {ex.Message}");
                  ClearKeysUI();
                   Dispatcher.UIThread.Post(() => {});
             }
             catch (Exception ex)
             {
                 Log($"ОШИБКА при генерации ключей: {ex.Message}");
                 ClearKeysUI();
                  Dispatcher.UIThread.Post(() => { });
             }
             finally
             {
                 EnableAllActionButtons(true); // Включаем кнопки обратно
             }
         }


         // --- Внутренний метод для генерации пары ключей (остается синхронным) ---
         private (long n, long e, long d) GenerateKeyPairInternal(long pVal, long qVal)
         {
              // ... (код GenerateKeyPairInternal остается без изменений) ...
               // Вычисляем N = p * q с проверкой на переполнение
                 long n;
                 try
                 {
                    n = checked(pVal * qVal); // Используем checked для явного контроля переполнения
                 }
                 catch (OverflowException)
                 {
                     throw new OverflowException($"Переполнение при вычислении N = p * q ({pVal} * {qVal}). Выберите меньшие p и q.");
                 }

                 // Вычисляем phi(n) = (p-1) * (q-1) с проверкой на переполнение
                 long p_1 = pVal - 1;
                 long q_1 = qVal - 1;
                 long phi;
                 try
                 {
                     phi = checked(p_1 * q_1);
                 }
                  catch (OverflowException)
                 {
                      throw new OverflowException($"Переполнение при вычислении Phi = (p-1)*(q-1) ({p_1} * {q_1}).");
                 }

                 if (phi <= 2) throw new ArgumentException("Значение Phi слишком мало, невозможно найти экспоненту 'e'.");

                 // Выбираем открытую экспоненту 'e' (часто 65537, но мы найдем меньшую)
                 // Ищем первое нечетное e > 1, взаимно простое с phi
                 long e = 3;
                 while (CryptoUtils.Gcd(e, phi) != 1)
                 {
                     e += 2; // Проверяем только нечетные
                     if (e >= phi) // Это не должно произойти для phi > 2
                         throw new ArgumentException($"Не удалось найти подходящую открытую экспоненту 'e' < Phi ({phi}).");
                 }

                 // Вычисляем секретную экспоненту 'd' такую, что d*e ≡ 1 (mod phi)
                 long d = CryptoUtils.ModInverse(e, phi); // ModInverse выбросит исключение, если обратного нет

                 return (n, e, d);
         }


        // 4. Шифрование и Подпись (!!! ПЕРЕДЕЛАТЬ НА ASYNC !!! - Хотя шифрование обычно быстрее генерации)
        // Шифрование/подпись могут быть достаточно быстрыми для коротких сообщений и ключей long.
        // Если они тоже вызывают зависания, переделайте их асинхронными по аналогии с GeneratePrimes_Click.
        // Пока оставим синхронными для простоты, но добавим выключение/включение кнопок.
        private void EncryptSign_Click(object sender, RoutedEventArgs e)
        {
            Log("Начало шифрования и подписи сообщения...");
            string message = OriginalMessageTextBox.Text;
            if (string.IsNullOrEmpty(message))
            {
                Log("Ошибка: Исходное сообщение не может быть пустым.");
                return;
            }
            if (receiverN == 0 || receiverE == 0 || senderN == 0 || senderD == 0)
            {
                Log("Ошибка: Ключи отправителя или получателя не сгенерированы.");
                return;
            }

            EnableAllActionButtons(false); // Выключаем кнопки
            ClearEncryptionUI(); // Очищаем старые результаты
            ClearDecryptionUI();
             Dispatcher.UIThread.Post(() => { });


            try
            {
                 // --- Шаг 1: Шифрование сообщения ОТКРЫТЫМ ключом ПОЛУЧАТЕЛЯ ---
                 Log($" -> Шифрование сообщения ключом Получателя (N={receiverN}, e={receiverE})...");
                 encryptedMessageNumeric.Clear();
                 StringBuilder encryptedHexBuilder = new StringBuilder();
                 foreach (char c in message)
                 {
                     long charCode = (long)c;
                     if (charCode >= receiverN)
                     {
                         throw new ArgumentException($"Ошибка: Код символа '{c}' ({charCode}) >= N получателя ({receiverN}). Шифрование невозможно.");
                     }
                     long encryptedChar = CryptoUtils.EncryptLong(charCode, receiverE, receiverN);
                     encryptedMessageNumeric.Add(encryptedChar);
                     encryptedHexBuilder.Append(encryptedChar.ToString("X") + " ");
                 }
                 // Обновляем UI в UI потоке
                 Dispatcher.UIThread.Post(() => {
                    EncryptedDataTextBox.Text = encryptedHexBuilder.ToString().TrimEnd();
                 });
                 Log($"    Зашифрованное сообщение (Hex): {encryptedHexBuilder.ToString().TrimEnd()}"); // Логируем результат


                 // --- Шаг 2: Вычисление хеша ИСХОДНОГО сообщения ---
                 Log($" -> Вычисление хеша исходного сообщения (mod {senderN})...");
                 long hash = CryptoUtils.SimpleHash(message, senderN);
                 Log($"    Хеш: {hash}");
                 if (hash >= senderN)
                 {
                     throw new ArgumentException($"Критическая ошибка: Хеш сообщения ({hash}) >= N отправителя ({senderN}). Подпись невозможна.");
                 }

                 // --- Шаг 3: Подпись хеша СЕКРЕТНЫМ ключом ОТПРАВИТЕЛЯ ---
                 Log($" -> Создание подписи хеша ({hash}) секретным ключом Отправителя (d={senderD}, N={senderN})...");
                 signatureNumeric = CryptoUtils.EncryptLong(hash, senderD, senderN);
                 // Обновляем UI в UI потоке
                  Dispatcher.UIThread.Post(() => {
                     SignatureTextBox.Text = signatureNumeric.ToString("X");
                  });
                 Log($"    Подпись (Hex): {signatureNumeric.ToString("X")}"); // Логируем результат


                 Log("--- Имитация Пересылки Получателю ---");
                 Log("Отправлено: Зашифрованное сообщение + Подпись");
                 Log("------------------------------------");
                  Dispatcher.UIThread.Post(() => {});

            }
            catch (ArgumentException ex)
            {
                 Log($"ОШИБКА (Неверный аргумент): {ex.Message}");
                 ClearEncryptionUI();
                  Dispatcher.UIThread.Post(() => { });
            }
            catch (Exception ex)
            {
                Log($"ОШИБКА при шифровании/подписи: {ex.Message}");
                ClearEncryptionUI();
                 Dispatcher.UIThread.Post(() => { });
            }
             finally
             {
                  EnableAllActionButtons(true); // Включаем кнопки обратно
             }
        }

        // 5. Расшифрование и Проверка Подписи (Аналогично п.4 - пока оставим синхронным)
         private void DecryptVerify_Click(object sender, RoutedEventArgs e)
         {
              Log("Начало расшифрования и проверки подписи (на стороне Получателя)...");
              if (encryptedMessageNumeric == null || encryptedMessageNumeric.Count == 0 || signatureNumeric == 0)
              {
                  Log("Ошибка: Нет данных для расшифрования или подписи не существует.");
                  return;
              }
              if (receiverN == 0 || receiverD == 0 || senderN == 0 || senderE == 0)
              {
                  Log("Ошибка: Ключи получателя или отправителя (открытый) недействительны или не загружены.");
                  return;
              }

             EnableAllActionButtons(false); // Выключаем кнопки
             ClearDecryptionUI(); // Очищаем старые результаты
              Dispatcher.UIThread.Post(() => {});

              try
              {
                   // --- Шаг 1: Расшифрование сообщения СЕКРЕТНЫМ ключом ПОЛУЧАТЕЛЯ ---
                  Log($" -> Расшифрование сообщения секретным ключом Получателя (d={receiverD}, N={receiverN})...");
                  StringBuilder decryptedTextBuilder = new StringBuilder();
                  foreach (long encryptedChar in encryptedMessageNumeric)
                  {
                      long decryptedChar = CryptoUtils.DecryptLong(encryptedChar, receiverD, receiverN);
                      if (decryptedChar > char.MaxValue || decryptedChar < char.MinValue)
                      {
                           Log($"    Предупреждение: Расшифрованное значение {decryptedChar} выходит за диапазон char. Замена на '?'.");
                           decryptedTextBuilder.Append('?');
                      }
                      else
                      {
                          decryptedTextBuilder.Append((char)decryptedChar);
                      }
                  }
                  string decryptedMessage = decryptedTextBuilder.ToString();
                  // Обновляем UI
                  Dispatcher.UIThread.Post(() => {
                       DecryptedMessageTextBox.Text = decryptedMessage;
                  });
                  Log($"    Расшифрованное сообщение: '{decryptedMessage}'");


                  // --- Шаг 2: Вычисление хеша РАСШИФРОВАННОГО сообщения ---
                   Log($" -> Вычисление хеша расшифрованного сообщения (mod {senderN})...");
                  long receivedHash = CryptoUtils.SimpleHash(decryptedMessage, senderN);
                   Log($"    Вычисленный хеш: {receivedHash}");

                  // --- Шаг 3: Проверка подписи ОТКРЫТЫМ ключом ОТПРАВИТЕЛЯ ---
                   Log($" -> Проверка подписи ({signatureNumeric.ToString("X")} Hex) открытым ключом Отправителя (e={senderE}, N={senderN})...");
                  long verifiedHash = CryptoUtils.DecryptLong(signatureNumeric, senderE, senderN);
                   Log($"    Хеш, восстановленный из подписи: {verifiedHash}");

                  // --- Шаг 4: Сравнение хешей ---
                  Log(" -> Сравнение вычисленного хеша и хеша из подписи...");
                  string verificationResult;
                  if (receivedHash == verifiedHash)
                  {
                      verificationResult = "УСПЕХ: Подпись действительна.";
                  }
                  else
                  {
                       verificationResult = "ПРОВАЛ: Подпись недействительна!";
                  }
                   // Обновляем UI
                  Dispatcher.UIThread.Post(() => {
                       VerificationStatusTextBlock.Text = verificationResult;
                       
                  });
                  Log($"    Результат: {verificationResult}");

              }
              catch (Exception ex)
              {
                  Log($"ОШИБКА при расшифровании/проверке: {ex.Message}");
                  ClearDecryptionUI();
                   Dispatcher.UIThread.Post(() => { });
              }
              finally
              {
                  EnableAllActionButtons(true); // Включаем кнопки обратно
              }
         }

    } // Конец класса MainWindow
} // Конец namespace
