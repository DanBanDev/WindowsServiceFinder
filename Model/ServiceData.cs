using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.IO;
using System.Text;
using System.Collections.ObjectModel;
using System.Management;
using System.ServiceProcess;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Xml.Serialization;
using System.Xml;


namespace Service_Finder.Model
{


    public partial class ServiceData : ObservableObject
    {
        public ServiceData()
        {
            serviceList = new ObservableCollection<ServiceController>(ServiceController.GetServices());
            
            loadFullCollection();

            LoadWatchlist();

            WatchListServiceCollection = new ObservableCollection<FullServiceProperties>(CreateFullWatchListCollection());

        }

        readonly string applicationPath = Path.Combine(Environment.CurrentDirectory, "mydata.xml");

        [ObservableProperty]
        ObservableCollection<ServiceController> serviceList;

        List<string> watchListServiceNameCollection { get; set; } = new List<string>();

        public ObservableCollection<FullServiceProperties> WatchListServiceCollection { get; set; }


        [ObservableProperty]
        //[NotifyPropertyChangedFor(nameof(FilteredList))]
        ObservableCollection<FullServiceProperties> fullServiceCollection = new();


       // public string[] ServiceDescriptions { get; set; }

       // public string[] ServicePathes { get; set; }

        public int NumberServices { get;  set; }

        public delegate MessageBoxResult OpenMessageBoxHandler(string message);
        public OpenMessageBoxHandler OpenMessageBox { get; set; }


        void loadFullCollection()
        {

            NumberServices= ServiceList.Count;
            //ServiceDescriptions = new string[NumberServices];
            //ServicePathes = new string[NumberServices];

            //FullServiceCollection = new ObservableCollection<FullServiceProperties>();

            for (var i = 0; i < NumberServices; i++)
            {
                using (var mngObject = new ManagementObject(new ManagementPath(string.Format("Win32_Service.Name='{0}'", ServiceList[i].ServiceName))))
                {
                    //ServiceDescriptions[i] = mngObject["Description"]?.ToString();
                    //ServicePathes[i]= mngObject["PathName"]?.ToString();

                    FullServiceCollection.Add(new FullServiceProperties() { Service = ServiceList[i], Description = mngObject["Description"]?.ToString() ?? "null", ServicePath = mngObject["PathName"]?.ToString() ?? "null" });
                }

            }
        }

       public void Save(string servicename, bool newServiceWillBeAdded)
        {
            try {

                if (newServiceWillBeAdded)
                    watchListServiceNameCollection.Add(servicename);
                else 
                    watchListServiceNameCollection.Remove(servicename);

                var serializer = new XmlSerializer(typeof(List<string>));
                 
                    using (var writer = new XmlTextWriter(applicationPath, Encoding.UTF8))
                    {
                        serializer.Serialize(writer, watchListServiceNameCollection);
                   
                    }
                }

            catch (Exception e)
            {
                //MessageBox.Show(e.ToString());
                OpenMessageBox(e.ToString());
            }
        }

        void LoadWatchlist()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<string>));

                using (var reader = new XmlTextReader(applicationPath))
                {
                    watchListServiceNameCollection = (List<string>?)serializer.Deserialize(reader) ?? new List<string>();
                }

            }
            catch (Exception e)
            {
                //MessageBox.Show(e.ToString());
                OpenMessageBox(e.ToString());
            }


        }

        List<FullServiceProperties> CreateFullWatchListCollection()
        {

           return watchListServiceNameCollection.Count>0 ? FullServiceCollection.Where(x => watchListServiceNameCollection.Contains(x.Service.ServiceName)).ToList(): new List<FullServiceProperties>();          

        }

        public class FullServiceProperties
        {
            public ServiceController Service { get; set; } = new ServiceController();   
            string description = String.Empty;
            public string Description
            {
                get => description;
                set
                {

                    if (value != null)
                        description = value;
                    else
                        description = "";
                }
            }
            public string ServicePath { get; set; } = String.Empty;

        }

    }


}

