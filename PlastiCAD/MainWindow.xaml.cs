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
        private PlacedPart selectedPlacedPart;
        private const double Scale = 2.0;
        private const double SnapDistance = 12.0;

        private bool isDragging = false;
        private Vector3 dragOffset = new Vector3();
        private Socket snapMovingSocket;
        private Socket snapOtherSocket;
        private PlacedPart snapOtherPart;
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

            TrySnap(selectedPlacedPart);
            RedrawScene();
        }

        private void BuildArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            BuildArea.ReleaseMouseCapture();

            if (snapMovingSocket != null &&
                snapOtherSocket != null &&
                !snapMovingSocket.IsConnected &&
                !snapOtherSocket.IsConnected)
            {
                assembly.Connections.Add(new Connection
                {
                    SocketA = snapMovingSocket,
                    SocketB = snapOtherSocket
                });

                snapMovingSocket.IsConnected = true;
                snapOtherSocket.IsConnected = true;

                snapMovingSocket.ConnectedTo = snapOtherSocket;
                snapOtherSocket.ConnectedTo = snapMovingSocket;

                StatusText.Text = "Verbunden";
            }
            else
            {
                StatusText.Text = "Bereit";
            }

            snapMovingSocket = null;
            snapOtherSocket = null;
            snapOtherPart = null;

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

            if (selectedPart is Pipe pipe)
            {
                foreach (Socket socket in pipe.Sockets)
                {
                    placed.Sockets.Add(new Socket
                    {
                        Index = socket.Index,
                        Name = socket.Name,
                        Position = new Vector3(
                            socket.Position.X,
                            socket.Position.Y,
                            socket.Position.Z),

                        Direction = new Vector3(
                            socket.Direction.X,
                            socket.Direction.Y,
                            socket.Direction.Z)
                    });
                }
            }
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

        private Vector3 GetWorldSocketPosition(PlacedPart placed, Socket socket)
        {
            return new Vector3(
                placed.Transform.Position.X + socket.Position.X * Scale,
                placed.Transform.Position.Y + socket.Position.Y * Scale,
                0);
        }
        private void TrySnap(PlacedPart movingPart)
        {
            Socket bestMovingSocket = null;
            Socket bestOtherSocket = null;

            PlacedPart bestOtherPart = null;

            double bestDistance = double.MaxValue;
            foreach (PlacedPart otherPart in assembly.PlacedParts)
            {
                // Sich selbst überspringen
                if (otherPart == movingPart)
                    continue;

                // Im Moment können wir nur Rohre snappen
                if (!(movingPart.Part is Pipe movingPipe))
                    continue;

                if (!(otherPart.Part is Pipe otherPipe))
                    continue;

                foreach (Socket movingSocket in movingPart.Sockets)
                {
                    foreach (Socket otherSocket in otherPart.Sockets)
                    {
                        if (movingSocket.Index == otherSocket.Index)
                            continue;
                        Vector3 movingPos = GetWorldSocketPosition(movingPart, movingSocket);
                        Vector3 otherPos = GetWorldSocketPosition(otherPart, otherSocket);

                        double distance = movingPos.DistanceTo(otherPos);

                        if (distance < bestDistance)
                        {
                            bestDistance = distance;

                            bestMovingSocket = movingSocket;
                            bestOtherSocket = otherSocket;

                            bestOtherPart = otherPart;
                        }
                    }
                }
            }
            if (bestDistance < SnapDistance)
            {
                snapMovingSocket = bestMovingSocket;
                snapOtherSocket = bestOtherSocket;
                snapOtherPart = bestOtherPart;
                Vector3 movingPos = GetWorldSocketPosition(movingPart, bestMovingSocket);
                Vector3 otherPos = GetWorldSocketPosition(bestOtherPart, bestOtherSocket);

                movingPart.Transform.Position.X += otherPos.X - movingPos.X;
                movingPart.Transform.Position.Y += otherPos.Y - movingPos.Y;

                // Nur verbinden, wenn beide Sockets noch frei sind
                if (!bestMovingSocket.IsConnected && !bestOtherSocket.IsConnected)
                {
                    // assembly.Connections.Add(new Connection
                    // {
                    //     SocketA = bestMovingSocket,
                    //     SocketB = bestOtherSocket
                    // });

                    // bestMovingSocket.IsConnected = true;
                    // bestOtherSocket.IsConnected = true;

                    // bestMovingSocket.ConnectedTo = bestOtherSocket;
                    // bestOtherSocket.ConnectedTo = bestMovingSocket;
                    snapMovingSocket = bestMovingSocket;
                    snapOtherSocket = bestOtherSocket;
                    snapOtherPart = bestOtherPart;
                    StatusText.Text = "Verbunden";
                }
            }
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

    

    
