using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp17
{
    public partial class MainWindow : Window
    {
        private static SemaphoreSlim semaphore = new SemaphoreSlim(3, 3); // Используем SemaphoreSlim для асинхронности
        private Random random = new Random();
        private Mutex mutex;
        private bool createdNew;

        public MainWindow()
        {
            InitializeComponent();
            mutex = new Mutex(false, "DB744E26-72C1-4F2A-8BF8-5C31980953C7", out createdNew);
        }

        private async Task RunThreadsAsync()
        {
            if (!await semaphore.WaitAsync(0))
            {
                MessageBox.Show("Программа не может запустить более 3 потоков.");
                return;
            }

            try
            {
                // Стартуем первый поток
                await Task.Run(() => ThreadFunction1());
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task ThreadFunction1()
        {
            await Task.Yield(); // Позволяет другим потокам работать

            mutex.WaitOne();
            uiContext.Send(d => txtStatus1.Text = "Поток 1: Запущен", null);

            int[] numbers = GenerateRandomNumbers();
            await WriteNumbersToFile("number.txt", numbers);

            mutex.ReleaseMutex();
        }

        private int[] GenerateRandomNumbers()
        {
            int[] numbers = new int[100];
            for (int i = 0; i < numbers.Length; i++)
            {
                numbers[i] = random.Next(i, 100);
            }
            return numbers;
        }

        private async Task WriteNumbersToFile(string fileName, int[] numbers)
        {
            await Task.Run(() =>
            {
                File.WriteAllLines(fileName, numbers.Select(n => n.ToString()));
            });
            uiContext.Send(d => txtStatus1.Text = "Поток 1: Записал числа в файл", null);
        }

        private async Task ThreadFunction2()
        {
            await Task.Yield(); // Позволяет другим потокам работать

            mutex.WaitOne();
            uiContext.Send(d => txtStatus2.Text = "Поток 2: Запущен", null);

            if (!File.Exists("number.txt"))
            {
                uiContext.Send(d => txtStatus2.Text = "Поток 2: Файл с числами не найден!", null);
                mutex.ReleaseMutex();
                return;
            }

            var numbers = await ReadNumbersFromFile("number.txt");
            var primeNumbers = numbers.Where(IsPrime).ToArray();

            await WriteNumbersToFile("primeNumbers.txt", primeNumbers);

            mutex.ReleaseMutex();
        }

        private async Task<int[]> ReadNumbersFromFile(string fileName)
        {
            return await Task.Run(() =>
            {
                string[] lines = File.ReadAllLines(fileName);
                return lines.Select(int.Parse).ToArray();
            });
        }

        private async Task ThreadFunction3()
        {
            await Task.Yield(); // Позволяет другим потокам работать

            mutex.WaitOne();
            uiContext.Send(d => txtStatus3.Text = "Поток 3: Запущен", null);

            if (!File.Exists("primeNumbers.txt"))
            {
                uiContext.Send(d => txtStatus3.Text = "Поток 3: Файл с простыми числами не найден!", null);
                mutex.ReleaseMutex();
                return;
            }

            var primeNumbers = await ReadNumbersFromFile("primeNumbers.txt");
            var numbersEndingIn7 = primeNumbers.Where(n => n % 10 == 7).ToArray();

            await WriteNumbersToFile("primeNumbersEndingIn7.txt", numbersEndingIn7);

            mutex.ReleaseMutex();
        }

        private bool IsPrime(int number)
        {
            if (number < 2)
                return false;

            for (int i = 2; i <= Math.Sqrt(number); i++)
            {
                if (number % i == 0)
                    return false;
            }

            return true;
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtStatus1.Text = "Поток 1: Ожидает";
                txtStatus2.Text = "Поток 2: Ожидает";
                txtStatus3.Text = "Поток 3: Ожидает";

                await RunThreadsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}
