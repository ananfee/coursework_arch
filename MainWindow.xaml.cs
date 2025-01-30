using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using plakplak.Controllers;
using plakplak.Models.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.IO;
using Microsoft.Office.Interop.Word;
using System.Reflection;
using Word = Microsoft.Office.Interop.Word;
using Microsoft.Win32;
using OxyPlot.Series;
using OxyPlot.Wpf;
using OxyPlot;
using System.Collections.Generic;
using plakplak.ViewModels;


namespace plakplak
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}


