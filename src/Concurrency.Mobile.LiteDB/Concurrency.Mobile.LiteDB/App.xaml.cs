using Concurrency.LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Concurrency.Mobile.LiteDB
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts

            // Very quickly you'll get a corrupt database
            var cache = new CacheLiteDB(FileSystem.CacheDirectory, "cache.db");
            var tasks = Enumerable.Range(1, 1000).Select(i => CacheTask.Work(cache, i)).ToList();
            Task.WhenAll(tasks).Wait();
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
