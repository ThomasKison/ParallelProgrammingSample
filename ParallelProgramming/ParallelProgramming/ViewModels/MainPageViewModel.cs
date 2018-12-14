using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ParallelProgramming.ViewModels
{
    class MainPageViewModel : BindableBase
    {
        private int _downloadedBytes;
        public int DownloadedBytes
        {
            get { return _downloadedBytes; }
            set { SetProperty(ref _downloadedBytes, value); }
        }

        public ObservableCollection<string> Files { get; set; } = new ObservableCollection<string>();

        private ICommand _downloadFileCommand;
        public ICommand DownloadFileCommand =>
            _downloadFileCommand ?? (_downloadFileCommand = new DelegateCommand<string>(ExecuteDownloadFileCommand));
        int _startId = 0;
        SemaphoreSlim _sem_executeDownloadFile = new SemaphoreSlim(1); // you can adjust the numbers of concurrent downloads 
        async void ExecuteDownloadFileCommand(string cnt)
        {
            //Thread.Sleep(5000); // this would block the UI-Thread
            for (int i = 0; i < int.Parse(cnt); i++)
            {
                await _sem_executeDownloadFile.WaitAsync();
                try
                //lock (this) // lock is not working when using async/await
                {
                    var startId = _startId++;
                    Files.Add($"{startId} start");
                    var url = await GetUrl();
                    using (var cl = new HttpClient())
                    {
                        Uri uri = new Uri(url);
                        try
                        {
                            var data = await cl.GetByteArrayAsync(uri);
                            Files.Add($"{startId} {uri}");
                            await Task.Delay(10);
                            DownloadedBytes += data.Length;
                        }
                        catch (Exception ex)
                        {
                            Files.Add($"{startId} error: {ex.Message}");
                        }

                    }
                }
                finally
                {
                    _sem_executeDownloadFile.Release();
                }
                Thread.Sleep(5000); // this would block the UI-Thread too, because we are in the same thread context
            }

        }
        Random _rnd = new Random();

        // just some random URLs
        string[] _urls = new[] {
            "https://sd.keepcalm-o-matic.co.uk/i-w600/keep-calm-and-let-the-dispatcher-handle-it.jpg",
            "https://jira.it2media.de/browse/BFB-1309",
            "https://www.google.de",
            "https://www.it2media.de",

        };
        private async Task<string> GetUrl()
        {
            Debug.WriteLine("GetUrl: " + Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(TimeSpan.FromSeconds(_rnd.Next(1, 2)));
            Debug.WriteLine("GetUrl: " + Thread.CurrentThread.ManagedThreadId);
            return _urls[_rnd.Next(0,_urls.Length-1)];
        }


        private DelegateCommand _calculatePrimeCommand;
        public DelegateCommand CalculatePrimeCommand =>
            _calculatePrimeCommand ?? (_calculatePrimeCommand = new DelegateCommand(ExecuteCalculatePrimeCommand));

        async void ExecuteCalculatePrimeCommand()
        {
            PrimeService ps = new PrimeService();
            Files.Clear();
            // correct solution
            var numbers = await ps.GetPrimeNumbersAsync(1, 10000); // not faster, but not blocking (its even slower): 40ms

            // eventhough these 2 statements only take 30ms, those will block the ui
            // we can avoid it, by using Task.Start(() => {..}) here
            // correct solution
            numbers = ps.GetPrimeNumbers(1, 10000); // 30ms
            // wrong order
            numbers = ps.GetPrimeNumbersParallel(1, 10000); 
            // first call: 25ms
            // very fast at second call<. 4ms (Thread creation is very performance intensive)
            foreach (var prime in numbers)
            {
                Files.Add(prime.ToString());
            }
        }
     
    }
}
