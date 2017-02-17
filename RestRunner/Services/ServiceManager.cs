using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Views;
using Microsoft.Practices.ServiceLocation;
using RestRunner.Design;

namespace RestRunner.Services
{
    public static class ServiceManager
    {
        //called within ViewModelLocator
        public static void RegisterServices()
        {
            if (ViewModelBase.IsInDesignModeStatic)
            {
                SimpleIoc.Default.Register<ICommandService, DesignCommandService>();
                SimpleIoc.Default.Register<ICommandChainService, DesignCommandChainService>();
                SimpleIoc.Default.Register<IEnvironmentService, DesignEnvironmentService>();
                SimpleIoc.Default.Register<IUserStateService, DesignUserStateService>();
            }
            else
            {
                SimpleIoc.Default.Register<ICommandService, CommandService>();
                SimpleIoc.Default.Register<ICommandChainService, CommandChainService>();
                SimpleIoc.Default.Register<IEnvironmentService, EnvironmentService>();
                SimpleIoc.Default.Register<IUserStateService, UserStateService>();
            }
        }

        //public static ICommandCategoryService CommandCategoryChain => ServiceLocator.Current.GetInstance<ICommandCategoryService>();

        //public static ICommandChainService CommandChain => ServiceLocator.Current.GetInstance<ICommandChainService>();

        //public static ICommandService Command => ServiceLocator.Current.GetInstance<ICommandService>();
    }
}
