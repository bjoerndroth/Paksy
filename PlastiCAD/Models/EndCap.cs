namespace PlastiCAD.Models
{
    public class EndCap : Part
    {
        public double OuterDiameter { get; set; }

        public double Length { get; set; }

        public EndCap()
        {
            Id = "EC001";
            Name = "Endkappe";
            Description = "Gelbe Endkappe für offene Rohrenden";

            OuterDiameter = 12.0;
            Length = 6.0;

            InitializeSockets();
        }

        private void InitializeSockets()
        {
            Sockets.Add(new Socket
            {
                Index = 0,
                Name = "Kappenanschluss",
                Face = Face.Right,
                Position = new Vector3(),
                Direction = new Vector3(1, 0, 0),
                Owner = this
            });
        }
    }
}