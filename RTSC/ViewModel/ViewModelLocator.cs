/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:TK158"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using GalaSoft.MvvmLight.Ioc;
using CommonServiceLocator;
using RTSC.Devices.TA2007.ViewModel;
using RTSC.Devices.TA2008.ViewModel;
using RTSC.Devices.Debug.ViewModel;
using RTSC.Devices.TA1004M1.ViewModel;
using RTSC.Devices.TA2006.ViewModel;
using RTSC.DialogWindows;

namespace RTSC.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register<MainWindowViewModel>();
            SimpleIoc.Default.Register<DebugViewModel>();
            SimpleIoc.Default.Register<TA1004M1ViewModel>();
            SimpleIoc.Default.Register<TA2006ViewModel>();
            SimpleIoc.Default.Register<TA2007ViewModel>();
            SimpleIoc.Default.Register<TA2008ViewModel>();
            SimpleIoc.Default.Register<TA2007ChannelSelectionViewModel>();
        }

        public MainWindowViewModel Main
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MainWindowViewModel>();
            }
        }
        public DebugViewModel ManualTest
        {
            get
            {
                return ServiceLocator.Current.GetInstance<DebugViewModel>();
            }
        }
        public TA1004M1ViewModel TA1004M1
        {
            get
            {
                return ServiceLocator.Current.GetInstance<TA1004M1ViewModel>();
            }
        }
        public TA2006ViewModel TA2006
        {
            get
            {
                return ServiceLocator.Current.GetInstance<TA2006ViewModel>();
            }
        }
        public TA2007ViewModel TA2007
        {
            get
            {
                return ServiceLocator.Current.GetInstance<TA2007ViewModel>();
            }
        }
        public TA2008ViewModel TA2008
        {
            get
            {
                return ServiceLocator.Current.GetInstance<TA2008ViewModel>();
            }
        }
        public TA2007ChannelSelectionViewModel TA2007ChannelSelection
        {
            get
            {
                return ServiceLocator.Current.GetInstance<TA2007ChannelSelectionViewModel>();
            }
        }

        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}