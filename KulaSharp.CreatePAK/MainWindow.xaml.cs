using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;

using System.IO;
using Microsoft.Win32;

namespace KulaSharp.CreatePAK
{
    public partial class MainWindow : Window
    {
        struct FileEntry
        {
            public string name, path;
        }

        List<FileEntry> fileEntries = new List<FileEntry>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                int selectedEntryIndex = InputFileList.SelectedIndex;
                if (selectedEntryIndex == -1) return;
                InputFileList.Items.RemoveAt(selectedEntryIndex);
                InputFileList.SelectedIndex = InputFileList.Items.Count - 1;
                fileEntries.RemoveAt(selectedEntryIndex);
                UpdateStatusLabel();
            }
        }

        private void InputFileList_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
        }

        private void InputFileList_DragDrop(object sender, DragEventArgs e)
        {
            string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            foreach (string filePath in filePaths)
            {
                FileEntry fileEntry;
                fileEntry.name = Path.GetFileName(filePath);
                fileEntry.path = filePath;
                fileEntries.Add(fileEntry);
                InputFileList.Items.Add(fileEntry.name);
            }
            UpdateStatusLabel();
        }

        private void OpenFilesButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Title = "Select Level Files";
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filePath in openFileDialog.FileNames)
                {
                    FileEntry fileEntry;
                    fileEntry.name = Path.GetFileName(filePath);
                    fileEntry.path = filePath;
                    fileEntries.Add(fileEntry);
                    InputFileList.Items.Add(fileEntry.name);
                }
                UpdateStatusLabel();
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (InputFileList.Items.Count == 0) if (MessageBox.Show("Are You Sure You Would Like to Export an Empty PAK File?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
            List<string> filePaths = new List<string>();
            foreach (FileEntry fileEntry in fileEntries) filePaths.Add(fileEntry.path);
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "OUTPUT.PAK";
            saveFileDialog.Filter = "PAK File|*.PAK";
            saveFileDialog.Title = "Export PAK File";
            if (saveFileDialog.ShowDialog() == true)
            {
                if (CreatePAKFile(filePaths, saveFileDialog.FileName)) MessageBox.Show("Successfully Exported " + filePaths.Count + " File(s) Into PAK File!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                else MessageBox.Show("Failed to Export " + filePaths.Count + " File(s) Into PAK File!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedEntryIndex = InputFileList.SelectedIndex;
            if (selectedEntryIndex == -1 || selectedEntryIndex == 0 || selectedEntryIndex - 1 == InputFileList.Items.Count) return;
            FileEntry fileEntry = fileEntries[selectedEntryIndex];
            InputFileList.Items.RemoveAt(selectedEntryIndex);
            InputFileList.Items.Insert(selectedEntryIndex - 1, fileEntry.name);
            fileEntries.RemoveAt(selectedEntryIndex);
            fileEntries.Insert(selectedEntryIndex - 1, fileEntry);
            InputFileList.SelectedIndex = selectedEntryIndex - 1;
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedEntryIndex = InputFileList.SelectedIndex;
            if (selectedEntryIndex == -1 || selectedEntryIndex + 1 == InputFileList.Items.Count) return;
            FileEntry fileEntry = fileEntries[selectedEntryIndex];
            InputFileList.Items.RemoveAt(selectedEntryIndex);
            InputFileList.Items.Insert(selectedEntryIndex + 1, fileEntry.name);
            fileEntries.RemoveAt(selectedEntryIndex);
            fileEntries.Insert(selectedEntryIndex + 1, fileEntry);
            InputFileList.SelectedIndex = selectedEntryIndex + 1;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedEntryIndex = InputFileList.SelectedIndex;
            if (selectedEntryIndex == -1) return;
            InputFileList.Items.RemoveAt(selectedEntryIndex);
            InputFileList.SelectedIndex = InputFileList.Items.Count - 1;
            fileEntries.RemoveAt(selectedEntryIndex);
            UpdateStatusLabel();
        }

        public void UpdateStatusLabel()
        {
            if (InputFileList.Items.Count > 0) StatusLabel.Content = "Levels Opened: " + InputFileList.Items.Count;
            else StatusLabel.Content = "Open a Level File";
        }

        public bool CreatePAKFile(List<string> filePaths, string outputFilePath)
        {
            try
            {
                int inputFileCount = filePaths.Count;
                List<long> fileOffsets = new List<long>(), fileSizes = new List<long>(), fileNameOffsets = new List<long>();
                FileStream outputFile = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);
                outputFile.Write(BitConverter.GetBytes(inputFileCount), 0, 4);
                for (int i = 0; i < inputFileCount * 12; i++) outputFile.WriteByte(0);
                for (int i = 0; i < inputFileCount; i++)
                {
                    fileNameOffsets.Add(outputFile.Position);
                    byte[] fileName = Encoding.ASCII.GetBytes(Path.GetFileName(filePaths[i]));
                    outputFile.Write(fileName, 0, fileName.Length);
                    outputFile.Write(BitConverter.GetBytes(10), 0, 2);
                }
                for (int i = 0; i < inputFileCount; i++)
                {
                    byte[] levelFile = File.ReadAllBytes(filePaths[i]);
                    byte[] compressedLevelFile = Ionic.Zlib.ZlibStream.CompressBuffer(levelFile);
                    fileOffsets.Add(outputFile.Position);
                    fileSizes.Add(compressedLevelFile.Length);
                    outputFile.Write(compressedLevelFile, 0, compressedLevelFile.Length);
                }
                outputFile.Seek(4, SeekOrigin.Begin);
                for (int i = 0; i < inputFileCount; i++)
                {
                    outputFile.Write(BitConverter.GetBytes(fileOffsets[i]), 0, 4);
                    outputFile.Write(BitConverter.GetBytes(fileSizes[i]), 0, 4);
                }
                for (int i = 0; i < inputFileCount; i++) outputFile.Write(BitConverter.GetBytes(fileNameOffsets[i]), 0, 4);
                outputFile.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
