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
using PlastiCAD.Core;


namespace PlastiCAD
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>


    public partial class MainWindow : Window
    {
      

        private Assembly assembly = new Assembly();
        private Part selectedPart;
        private PlacedPart selectedPlacedPart;
        private const double Scale = 2.0;
        private const double SnapDistance = 12.0;

        private bool isDragging = false;
        private Vector3 dragOffset = new Vector3();
        private SnapResult currentSnap;
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

        private void BuildArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || selectedPlacedPart == null)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                isDragging = false;
                BuildArea.ReleaseMouseCapture();
                return;
            }

            Point p = e.GetPosition(BuildArea);

            selectedPlacedPart.Transform.Position.X = p.X - dragOffset.X;
            selectedPlacedPart.Transform.Position.Y = p.Y - dragOffset.Y;

            currentSnap = SnapEngine.FindBestSnap(
    assembly,
    selectedPlacedPart,
    Scale,
    SnapDistance);

            if (currentSnap != null)
            {
                SnapEngine.ApplySnap(
                    selectedPlacedPart,
                    currentSnap,
                    Scale);
            }

            RedrawScene();
        }

        private void BuildArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            BuildArea.ReleaseMouseCapture();

            if (currentSnap != null &&
                !currentSnap.MovingSocket.IsConnected &&
                !currentSnap.OtherSocket.IsConnected)
            {
                assembly.Connections.Add(new Connection
                {
                    SocketA = currentSnap.MovingSocket,
                    SocketB = currentSnap.OtherSocket
                });

                currentSnap.MovingSocket.IsConnected = true;
                currentSnap.OtherSocket.IsConnected = true;

                currentSnap.MovingSocket.ConnectedTo = currentSnap.OtherSocket;
                currentSnap.OtherSocket.ConnectedTo = currentSnap.MovingSocket;

                StatusText.Text = "Verbunden";
            }
            else
            {
                StatusText.Text = "Bereit";
            }

            currentSnap = null;

            RedrawScene();
        }
        private void BuildArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(BuildArea);

            // Prüfen, ob ein vorhandenes Teil angeklickt wurde
            selectedPlacedPart = GetPartAt(p);



            if (selectedPlacedPart != null)
            {
                DisconnectPart(selectedPlacedPart);
                isDragging = true;

                BuildArea.CaptureMouse();

                dragOffset = new Vector3(
                    p.X - selectedPlacedPart.Transform.Position.X,
                    p.Y - selectedPlacedPart.Transform.Position.Y,
                    0);

                StatusText.Text = "Bauteil ausgewählt";

                RedrawScene();
                return;
            }
           

            // Wenn kein Teil getroffen wurde und kein Bibliotheksteil ausgewählt ist
            if (selectedPart == null)
                return;


            PlacedPart placed = new PlacedPart();

            placed.Part = selectedPart;

            placed.Transform.Position = new Vector3(
                p.X,
                p.Y,
                0
            );

            placed.Sockets = selectedPart.CreateSockets();

            assembly.PlacedParts.Add(placed);

            RedrawScene();
        }
        private void RedrawScene()
        {
            BuildArea.Children.Clear();

            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                if (placed.Part is Pipe pipe)
                {
                    DrawPipe(placed, pipe);
                }
            }
        }

        private PlacedPart GetPartAt(Point p)
        {
            foreach (PlacedPart placed in assembly.PlacedParts)
            {
                if (placed.Part is Pipe pipe)
                {
                    double width = pipe.Length * Scale;
                    double height = pipe.OuterDiameter;

                    if (p.X >= placed.Transform.Position.X &&
                        p.X <= placed.Transform.Position.X + width &&
                        p.Y >= placed.Transform.Position.Y &&
                        p.Y <= placed.Transform.Position.Y + height)
                    {
                        return placed;
                    }
                }
            }

            return null;
        }

 
       
        private void DisconnectPart(PlacedPart part)
        {
            foreach (Connection connection in assembly.Connections.ToList())
            {
                if (part.Sockets.Contains(connection.SocketA) ||
                    part.Sockets.Contains(connection.SocketB))
                {
                    connection.SocketA.IsConnected = false;
                    connection.SocketB.IsConnected = false;

                    connection.SocketA.ConnectedTo = null;
                    connection.SocketB.ConnectedTo = null;

                    assembly.Connections.Remove(connection);
                }
            }
        }
        private void DrawPipe(PlacedPart placed, Pipe pipe)
        {
            Rectangle rect = new Rectangle();

            rect.Width = pipe.Length * Scale;
            rect.Height = pipe.OuterDiameter;

            if (placed == selectedPlacedPart)
                rect.Fill = Brushes.Gold;
            else
                rect.Fill = Brushes.Blue;

            Canvas.SetLeft(rect, placed.Transform.Position.X);
            Canvas.SetTop(rect, placed.Transform.Position.Y);

            BuildArea.Children.Add(rect);
            // linker Socket
            // linker Socket
            Ellipse leftSocket = new Ellipse();
            leftSocket.Width = 8;
            leftSocket.Height = 8;

            if (placed.Sockets[0].IsConnected)
                leftSocket.Fill = Brushes.Green;
            else
                leftSocket.Fill = Brushes.Red;

            Canvas.SetLeft(leftSocket,
                placed.Transform.Position.X + placed.Sockets[0].Position.X * Scale - 4);

            Canvas.SetTop(leftSocket,
                placed.Transform.Position.Y + placed.Sockets[0].Position.Y);


            BuildArea.Children.Add(leftSocket);

            // rechter Socket
            Ellipse rightSocket = new Ellipse();
            rightSocket.Width = 8;
            rightSocket.Height = 8;
            if (placed.Sockets[1].IsConnected)
                rightSocket.Fill = Brushes.Green;
            else
                rightSocket.Fill = Brushes.Red;
            Canvas.SetLeft(rightSocket,
           placed.Transform.Position.X + placed.Sockets[1].Position.X * Scale - 4);

            Canvas.SetTop(rightSocket,
                placed.Transform.Position.Y + placed.Sockets[1].Position.Y);

            BuildArea.Children.Add(rightSocket);
        }
    }
    }

    

    
