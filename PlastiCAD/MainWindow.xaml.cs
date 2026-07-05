using PlastiCAD.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace PlastiCAD
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>


    public partial class MainWindow : Window
    {
        private Assembly assembly = new Assembly();
        private Part selectedPart;

        public MainWindow()
        
        {
            //test
            InitializeComponent();

            PartLibrary.Initialize();

            foreach (Part part in PartLibrary.Parts)
            {
                PartsList.Items.Add(part.Name);
            }
        }

        private void PartsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedPart = PartLibrary.Parts[PartsList.SelectedIndex];

            StatusText.Text = "Ausgewählt: " + selectedPart.Name;
        }
        private void BuildArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (selectedPart == null)
                return;

            Point p = e.GetPosition(BuildArea);

            PlacedPart placed = new PlacedPart();

            placed.Part = selectedPart;

            placed.Position = new Vector3(
                p.X,
                p.Y,
                0
            );

            assembly.PlacedParts.Add(placed);

            RedrawScene();
        }
        private void RedrawScene()
        {
            BuildArea.Children.Clear();

            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                Rectangle rect = new Rectangle();

                rect.Width = 40;
                rect.Height = 8;
                rect.Fill = Brushes.Blue;

                Canvas.SetLeft(rect, placed.Position.X);
                Canvas.SetTop(rect, placed.Position.Y);

                BuildArea.Children.Add(rect);
            }
        }
    }
    }

    

    
