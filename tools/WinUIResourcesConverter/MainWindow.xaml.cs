﻿using Ookii.Dialogs.Wpf;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WinUIResourcesConverter
{
    public partial class MainWindow : Window
    {
        private string sourceDirectory;
        private string destinationDirectory;

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        public ObservableCollection<ResourcesFile> ResourcesFiles { get; } = new();

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(LoadResourcesFiles);
        }

        private void LoadResourcesFiles()
        {
            Dispatcher.Invoke(() =>
            {
                sourceDirectory = TbSourceDirectory.Text;
                ResourcesFiles.Clear();
            });

            if (!string.IsNullOrEmpty(sourceDirectory) && Directory.Exists(sourceDirectory))
            {
                var dirInfo = new DirectoryInfo(sourceDirectory);
                foreach (var resourceFileInfo in dirInfo.GetFiles($"{ResourcesFile.DefaultResourcesFileName}.resw", SearchOption.AllDirectories))
                {
                    Dispatcher.Invoke(() =>
                    {
                        ResourcesFile resourcesFile = new(resourceFileInfo.Directory.Name);
                        ResourcesFiles.Add(resourcesFile);
                    });
                }
            }

            Dispatcher.Invoke(() =>
            {
                totalResFiles = ResourcesFiles.Count;
                ProgressBar1.Value = convertedResFiles = 0;
                ProgressBar1.Maximum = totalResFiles;
            });
        }

        private async void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentPanel.IsEnabled = false;
            ProgressRing1.IsActive = true;

            destinationDirectory = TbDestinationDirectory.Text;
            await ConvertResourcesAsync();

            ProgressRing1.IsActive = false;
            MainContentPanel.IsEnabled = true;
        }

        public async Task ConvertResourcesAsync()
        {
            await Task.Run(ConvertResources);
        }

        private int totalResFiles;
        private int convertedResFiles;

        private void ConvertResources()
        {
            convertedResFiles = 0;

            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                DirectoryInfo destination = new(destinationDirectory);

                string controlName = destination.Parent.Name;

                string GetRelativePath(DirectoryInfo directory)
                {
                    StringBuilder stringBuilder = new();

                    bool IsProjectRoot(DirectoryInfo dir)
                    {
                        return string.Equals(dir.Name, nameof(ModernWpf), System.StringComparison.OrdinalIgnoreCase)
                        || string.Equals(dir.Name, nameof(ModernWpf) + "." + nameof(ModernWpf.Controls), System.StringComparison.OrdinalIgnoreCase);
                    }

                    while (!IsProjectRoot(directory))
                    {
                        stringBuilder.Insert(0, directory.Name + ".");
                        directory = directory.Parent;
                    }

                    stringBuilder.Insert(0, directory.Name + ".");
                    directory = directory.Parent;

                    return stringBuilder.ToString();
                }

                CodeGen codeGen = new(controlName, GetRelativePath(destination.Parent.Parent));
                codeGen.AppendFirstPart();

                ResourcesFile[] resFiles = null;

                Dispatcher.Invoke(() =>
                {
                    resFiles = ResourcesFiles.ToArray();
                });

                if (resFiles == null) return;

                foreach (var resFile in resFiles)
                {
                    resFile.HasConverted = RESXConverter.TryConvertReswToResx(resFile, sourceDirectory, destinationDirectory, resFile.IsDefaultResource ? codeGen : null);
                    convertedResFiles++;

                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar1.Value = convertedResFiles;
                    });
                }

                Dispatcher.Invoke(() =>
                {
                    TbCodeGen.Text = codeGen.GeneratedCode;
                });
            }
        }

        private void SelectSourceDirectory(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new VistaFolderBrowserDialog()
            {
                ShowNewFolderButton = true,
                Description = "Select the source directory",
                UseDescriptionForTitle = true
            };
            var result = folderBrowserDialog.ShowDialog();
            if (result.HasValue && result.Value == true)
            {
                TbSourceDirectory.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void SelectDestinationDirectory(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new VistaFolderBrowserDialog()
            {
                ShowNewFolderButton = true,
                Description = "Select the destination directory",
                UseDescriptionForTitle = true
            };
            var result = folderBrowserDialog.ShowDialog();
            if (result.HasValue && result.Value == true)
            {
                TbDestinationDirectory.Text = folderBrowserDialog.SelectedPath;
            }
        }
    }
}
