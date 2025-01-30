using plakplak.Controllers;
using plakplak.Models.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using OxyPlot.Series;
using OxyPlot;
using OxyPlot.Wpf;
using System.IO;
using GalaSoft.MvvmLight.CommandWpf;
using System.Diagnostics;
using Microsoft.Office.Interop.Word;

namespace plakplak.ViewModels
{
    public class MainWindowViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private readonly HtmlController _htmlController = new HtmlController();
        private readonly DataBaseController _dataBaseController = new DataBaseController();
        private ObservableCollection<Pokemons> _pokemons;
        private Pokemons _selectedPokemon;
        private Visibility _chartVisibility = Visibility.Collapsed;
        private PlotModel _chartModel;
        private string _chartFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "pokemon_types_chart.png");
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public MainWindowViewModel()
        {
            LoadPokemonsCommand = new RelayCommand(LoadPokemons);
            DeleteAllCommand = new RelayCommand(DeleteAllPokemons);
            DeleteSelectedCommand = new RelayCommand(DeleteSelectedPokemon);
            GenerateReportCommand = new RelayCommand(GenerateReport);
            CreateChartCommand = new RelayCommand(CreateChart);
            AddChartCommand = new RelayCommand(AddChartToReport);
        }
        public ObservableCollection<Pokemons> Pokemons
        {
            get { return _pokemons; }
            set
            {
                _pokemons = value;
                OnPropertyChanged(nameof(Pokemons));
            }
        }
        public PlotModel ChartModel
        {
            get { return _chartModel; }
            set
            {
                _chartModel = value;
                OnPropertyChanged(nameof(ChartModel));
            }
        }
        public Pokemons SelectedPokemon
        {
            get { return _selectedPokemon; }
            set
            {
                _selectedPokemon = value;
                OnPropertyChanged(nameof(SelectedPokemon));
            }
        }
        public ICommand LoadPokemonsCommand { get; }
        public ICommand DeleteAllCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand GenerateReportCommand { get; }
        public ICommand CreateChartCommand { get; }
        public ICommand AddChartCommand { get; }

        private async void LoadPokemons()
        {
            try
            {
                //using (var context = new haEntities())
                //{
                //    await System.Threading.Tasks.Task.Run(() => _dataBaseController.Delete(context));
                //}
                using (var context = new haEntities())
                {
                    if (context.Pokemons.Any())
                    {
                        MessageBox.Show("Данные о покемонах уже существуют в базе данных.");
                        LoadPokemonsFromDatabase(); 
                        return;
                    }
                }
                var pokemons = await System.Threading.Tasks.Task.Run(() => _htmlController.GetPokemons()); 
                LoadPokemonsFromDatabase();
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных из интернета: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при загрузке данных: {ex.Message}");
            }
        }
        private void LoadPokemonsFromDatabase()
        {
            using (var context = new haEntities())
            {
                var pokemons = context.Pokemons.Include("Abilities").Include("Types").ToList();
                Pokemons = new ObservableCollection<Pokemons>(pokemons);
            }
        }
        private async void DeleteAllPokemons()
        {
            try
            {
                using (var context = new haEntities())
                {
                    await System.Threading.Tasks.Task.Run(() => _dataBaseController.Delete(context));
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении данных: {ex.Message}");
            }
            LoadPokemonsFromDatabase();
            MessageBox.Show("Данные удалены.");
        }
        private async void DeleteSelectedPokemon()
        {
            if (SelectedPokemon != null)
            {
                try
                {
                    using (var context = new haEntities())
                    {
                        var pokemonToRemove = context.Pokemons.FirstOrDefault(p => p.Id == SelectedPokemon.Id);
                        if (pokemonToRemove != null)
                        {
                            context.Pokemons.Remove(pokemonToRemove);
                            await context.SaveChangesAsync();
                            Pokemons.Remove(SelectedPokemon);
                        }
                        else
                        {
                            MessageBox.Show("Покемон не найден в базе данных.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении покемона: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите покемона для удаления из списка.");
            }
        }
        private void GenerateReport()
        {
            if (SelectedPokemon == null)
            {
                MessageBox.Show("Пожалуйста, выберите покемона для отчета.");
                return;
            }
            string report = GenerateReport(SelectedPokemon);
            CreateWordDocument(report);
        }
        private string GenerateReport(Pokemons pokemon)
        {
            return $"Отчет для покемона: {pokemon.Name}\n\n" +
                        $"ID: {pokemon.Id}\n" +
                       $"Типы: {string.Join(", ", pokemon.Types.TypeName)}\n" +
                        $"Способность: {pokemon.Abilities.AbilityName}\n";
        }
        private void CreateWordDocument(string report)
        {
            string appDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string templatePath = System.IO.Path.Combine(appDirectory, "rep_template.docx");

            if (!System.IO.File.Exists(templatePath))
            {
                MessageBox.Show($"Файл шаблона не найден по пути {templatePath}.");
                return;
            }

            Microsoft.Office.Interop.Word.Application wordApp = new Microsoft.Office.Interop.Word.Application();
            Microsoft.Office.Interop.Word.Document wordDoc = null;

            try
            {
                wordDoc = wordApp.Documents.Add(templatePath);
                if (_selectedPokemon != null)
                {
                    FindAndReplace(wordDoc, "<<PokemonName>>", _selectedPokemon.Name, wordApp);
                    FindAndReplace(wordDoc, "<<PokemonId>>", _selectedPokemon.Id.ToString(), wordApp);
                    FindAndReplace(wordDoc, "<<PokemonTypes>>", string.Join(", ", _selectedPokemon.Types.TypeName), wordApp);
                    FindAndReplace(wordDoc, "<<PokemonAbility>>", _selectedPokemon.Abilities.AbilityName, wordApp);

                    string fileName = $"{_selectedPokemon.Name.Replace(" ", "_")}_{_selectedPokemon.Id}.docx";

                    string fullFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);

                    wordDoc.SaveAs2(fullFilePath);
                    //Process.Start(fullFilePath);
                    MessageBox.Show("Отчет создан.");

                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Выбранный покемон равен null.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании документа: {ex.Message}");
            }
            finally
            {
                if (wordDoc != null)
                {
                    ((Microsoft.Office.Interop.Word._Document)wordDoc).Close(Microsoft.Office.Interop.Word.WdSaveOptions.wdDoNotSaveChanges);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordDoc);
                    wordDoc = null;
                }
                if (wordApp != null)
                {
                    ((Microsoft.Office.Interop.Word._Application)wordApp).Quit(false, Microsoft.Office.Interop.Word.WdOriginalFormat.wdOriginalDocumentFormat);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                    wordApp = null;
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        private void FindAndReplace(Microsoft.Office.Interop.Word.Document doc, object findText, object replaceText, Microsoft.Office.Interop.Word.Application wordApp)
        {
            var findObject = wordApp.Selection.Find;
            findObject.ClearFormatting();
            findObject.Text = findText.ToString();
            findObject.Replacement.ClearFormatting();
            findObject.Replacement.Text = replaceText.ToString();
            findObject.Execute(Replace: Microsoft.Office.Interop.Word.WdReplace.wdReplaceAll);
        }
        private void AddChartToReport()
        {
            if (SelectedPokemon == null)
            {
                MessageBox.Show("Выберите покемона для добавления графика в отчёт.");
                return;
            }

            string reportFileName = $"{SelectedPokemon.Name.Replace(" ", "_")}_{SelectedPokemon.Id}.docx";
            string reportFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), reportFileName);

            if (!File.Exists(reportFilePath))
            {
                MessageBox.Show($"Отчет для покемона {SelectedPokemon.Name} не существует. Сначала создайте отчет.");
                return;
            }

            Microsoft.Office.Interop.Word.Application wordApp = null;
            Microsoft.Office.Interop.Word.Document wordDoc = null;
            try
            {
                wordApp = new Microsoft.Office.Interop.Word.Application();
                wordDoc = wordApp.Documents.Open(reportFilePath);

                Range range = wordDoc.Content;
                range.Collapse(WdCollapseDirection.wdCollapseEnd);

                range.InlineShapes.AddPicture(_chartFilePath, false, true);


                wordDoc.Save();
                MessageBox.Show($"График добавлен в отчет для {SelectedPokemon.Name}.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении графика в отчёт: {ex.Message}");
            }
            finally
            {
                if (wordDoc != null)
                {
                    wordDoc.Close();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordDoc);
                }
                if (wordApp != null)
                {
                    wordApp.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                }
            }
        }
        private void CreateChart()
        {
            var model = new PlotModel { Title = "Количество покемонов по типам" };
            var pieSeries = new PieSeries { StrokeThickness = 2.0, InsideLabelPosition = 0.8, AngleSpan = 360, StartAngle = 0 };

            if (Pokemons != null)
            {
                var typeCounts = Pokemons
                .GroupBy(p => p.Types.TypeName)
                .Select(group => new { TypeName = group.Key, Count = group.Count() });

                foreach (var typeCount in typeCounts)
                {
                    pieSeries.Slices.Add(new PieSlice(typeCount.TypeName, typeCount.Count) { IsExploded = false });
                }

                model.Series.Add(pieSeries);
            }
            else
            {
                MessageBox.Show("Список покемонов пуст, загрузите их.");
                return;
            }

            ChartModel = model;

            if (File.Exists(_chartFilePath))
            {
                MessageBox.Show($"График уже существует по пути: {_chartFilePath}.");
                return;
            }
            
            SaveChartAsPng(model, "pokemon_types_chart.png");

            MessageBox.Show("График создан и сохранен.");
        }
        private void SaveChartAsPng(PlotModel model, string fileName)
        {
            var pngExporter = new PngExporter { Width = 600, Height = 400 };
            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);

            using (var stream = File.Create(path))
            {
                pngExporter.Export(model, stream);
            }
        }
    }
}
