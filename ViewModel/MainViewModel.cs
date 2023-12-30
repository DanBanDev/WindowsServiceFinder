using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Service_Finder.Model;

namespace Service_Finder.ViewModel
{

    delegate bool FilterOption1(ServiceData.FullServiceProperties f, string j);
    delegate bool FilterOption2(ServiceData.FullServiceProperties f);

    public partial class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            ServiceDataObject = new ServiceData();
       
            filteredList = new ObservableCollection<ServiceData.FullServiceProperties>();

            filterOption1 = delegate (ServiceData.FullServiceProperties x, string y)
            {
                return (x.Service.ServiceName.ToLower().Contains(y) || x.Service.DisplayName.ToLower().Contains(y)); 
            };

            filterOption2 = delegate (ServiceData.FullServiceProperties x)
            {
                return true;
            };

            haveAdminRights = IsCurrentProcessAdmin();
            

        }

        public ServiceData ServiceDataObject { get; set; }

        [ObservableProperty]
        ObservableCollection<ServiceData.FullServiceProperties> filteredList;


        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ServiceActionCommand))]
        ServiceData.FullServiceProperties selectedItemMainListView;


        public delegate MessageBoxResult OpenMessageBoxHandler(string message);
        public OpenMessageBoxHandler OpenMessageBox { get; set; }

        bool haveAdminRights { get; } = false;

        string filter = String.Empty;
        public string Filter
        {
            get=> filter;
            set { 
                filter = value; 
                DoFiltering();  
            }   
        }

        byte option1_searchInColumn;
        public byte Option1_SearchInColumn
        {
            get => option1_searchInColumn; 
            set {
                option1_searchInColumn = value;
                if(option1_searchInColumn == 1)
                {
                    filterOption1 = delegate (ServiceData.FullServiceProperties x, string y)
                    {
                        return (x.Service.ServiceName.ToLower().Contains(y) || x.Service.DisplayName.ToLower().Contains(y));
                    };
                }
                else if(option1_searchInColumn == 2)
                {
                    filterOption1 = delegate (ServiceData.FullServiceProperties x, string y)
                    {
                        return (x.Description.ToLower().Contains(y) );
                    };
                }
                DoFiltering();
            }
        }

        byte option2_status;
        public byte Option2_Status {
            get=> option2_status;
            set {

                option2_status = value;
                switch(option2_status)
                {
                    case 1: filterOption2 = delegate (ServiceData.FullServiceProperties x)
                            {
                                return true;
                            };
                        break;
                    case 2: filterOption2 = delegate (ServiceData.FullServiceProperties x)
                            {
                                return x.Service.Status==ServiceControllerStatus.Running;
                            };
                        break;
                    case 3: filterOption2 = delegate (ServiceData.FullServiceProperties x)
                            {
                                return x.Service.Status == ServiceControllerStatus.Stopped;
                            };
                        break;
                }
                DoFiltering();
            } }

        FilterOption1 filterOption1;
        FilterOption2 filterOption2;

       

        bool CanExecuteServiceAction(object p)
        {
            switch ((string)p)
            {
                case "start": return (SelectedItemMainListView != null ? SelectedItemMainListView.Service.Status == ServiceControllerStatus.Stopped : false);
                    
                case "stop": return (SelectedItemMainListView != null ? SelectedItemMainListView.Service.CanStop : false);
                    
                case "pause": return (SelectedItemMainListView != null ? SelectedItemMainListView.Service.CanPauseAndContinue && SelectedItemMainListView.Service.Status == ServiceControllerStatus.Running : false);
                    
                case "continue": return (SelectedItemMainListView != null ? SelectedItemMainListView.Service.CanPauseAndContinue && SelectedItemMainListView.Service.Status == ServiceControllerStatus.Paused : false);
                    
                case "newStart": return (SelectedItemMainListView != null ? SelectedItemMainListView.Service.Status == ServiceControllerStatus.Running && SelectedItemMainListView.Service.CanStop : false);
                    
                default: return false;  

            }

        }

        [RelayCommand(CanExecute = nameof(CanExecuteServiceAction))]
        public async void ServiceAction(object p)
        {
  
            try
            {
                if (haveAdminRights)
                { 
                    switch ((string)p) 
                    { 
                    case "start":
                        SelectedItemMainListView.Service.Start();                    
                        await Task.Run(() => ShowProgress(ServiceControllerStatus.Running));                    
                                       
                        break;
                    case "stop":
                        SelectedItemMainListView.Service.Stop();
                        await Task.Run(() => ShowProgress(ServiceControllerStatus.Stopped));
                    
                        break;
                    case "pause":
                        SelectedItemMainListView.Service.Pause();
                        await Task.Run(() => ShowProgress(ServiceControllerStatus.Paused));
                    
                        break;
                    case "continue":
                        SelectedItemMainListView.Service.Continue();
                        await Task.Run(() => ShowProgress(ServiceControllerStatus.ContinuePending));
                    
                        break;
                    case "newStart":
                        SelectedItemMainListView.Service.Stop();
                        await Task.Run(() => ShowProgress(ServiceControllerStatus.Stopped));
                        if(SelectedItemMainListView.Service.Status== ServiceControllerStatus.Stopped)
                        {
                            SelectedItemMainListView.Service.Start();
                            await Task.Run(() => ShowProgress(ServiceControllerStatus.Running));
                        }
                    
                        break;

                    default: return;

                    }
                }
                else                
                    OpenMessageBox("The program requires admin rights to change the state of the service.");
            }
            catch(Exception e)
            {
                 OpenMessageBox(e.ToString());
            }

            ServiceActionCommand.NotifyCanExecuteChanged();
            DoFiltering();  


        }


        [RelayCommand]
        public void AddToWatchlistAction()
        {
            if(!ServiceDataObject.WatchListServiceCollection.Contains(SelectedItemMainListView))
            {
                ServiceDataObject.WatchListServiceCollection.Add(SelectedItemMainListView);
                ServiceDataObject.Save(SelectedItemMainListView.Service.ServiceName, true /*new Service is added*/);
            }
        }

        [RelayCommand]
        public void RemoveFromWatchlistAction()
        {
            try { 

                ServiceDataObject.Save(SelectedItemMainListView.Service.ServiceName, false /*Service is removed*/);
                ServiceDataObject.WatchListServiceCollection.Remove(SelectedItemMainListView);
            }
            catch(Exception e)
            {
                //MessageBox.Show(e.ToString());
                OpenMessageBox(e.ToString());
            }
        }



        [ObservableProperty]
        private int progressValue;

        public void ShowProgress(ServiceControllerStatus desiredStatus)
        {
 
            double currentValue = 0; // actual advance in percent          
            double virtualtime = 0;
            int statuscounter = 0;

            while (currentValue < 0.798d) //Loop breaks after 6 seconds (0.798 is reached after 600 loops with 20ms sleep)
            {
               
                currentValue = 0.8 - Math.Exp(-(virtualtime + 2.231) / 10);

                virtualtime += 0.2d;    //reach round about 0.75 at 2000ms ( 20ms sleep by 100 loopcounts).

                ProgressValue = (int)currentValue*100;

                if (statuscounter == 10)
                {
                    statuscounter = 0;
                    SelectedItemMainListView.Service.Refresh();
                    if (SelectedItemMainListView.Service.Status == desiredStatus)
                        break;
                }

                statuscounter++;

                Thread.Sleep(20);
            }

            if (SelectedItemMainListView.Service.Status == desiredStatus)
            {
                double incrementSteps = (100 - currentValue)/10;     //10 steps in round about 200ms
                for(int i=0;i<=10;i++)    //close gap to 100%
                {
                    currentValue += incrementSteps;
                    ProgressValue = (int)currentValue;
                    Thread.Sleep(20);
                }
                ProgressValue = 0;
                
            }
            else { 
                MessageBox.Show("Error. The action could not be executed.");
                ProgressValue = 0;
            }

        }




        private void DoFiltering()
        {
            this.FilteredList.Clear();
            string value = this.filter?.ToLower() ?? "";
            foreach (var item in ServiceDataObject.FullServiceCollection)
            {
                if ( ( String.IsNullOrEmpty(Filter)  ||  filterOption1(item, Filter) )  &&  filterOption2(item) )                  
                {
                    this.FilteredList.Add(item);
                }
            }
        }

        public bool IsCurrentProcessAdmin()
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

    }
}
