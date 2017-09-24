using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using FlowSharpLib;
using FlowSharpServiceInterfaces;

using Clifton.Kademlia;

namespace KademliaDemo
{
    public partial class MainForm : Form
    {
        protected List<Dht> dhts;
        protected List<Dht> knownPeers;
        protected List<Rectangle> dhtPos;
        protected Random rnd = new Random(4);
        protected int peerBootrappingIdx = 0;

        protected const int NUM_DHT = 60;
        protected const int ITEMS_PER_ROW = 10;
        protected const int XOFFSET = 30;
        protected const int YOFFSET = 30;
        protected const int SIZE = 30;
        protected const int XSPACING = 60;
        protected const int YSPACING = 80;
        protected const int JITTER = 15;
        protected const int NUM_KNOWN_PEERS = 5;

        public MainForm()
        {
            InitializeComponent();
            InitializeFlowSharp();
            InitializeDhts();
            InitializeKnownPeers();
            Shown += OnShown;
        }

        protected void InitializeFlowSharp()
        {
            var canvasService = Program.ServiceManager.Get<IFlowSharpCanvasService>();
            canvasService.CreateCanvas(pnlFlowSharp);
            canvasService.ActiveController.Canvas.EndInit();
            canvasService.ActiveController.Canvas.Invalidate();

            // Initialize Toolbox so we can drop shapes
            IFlowSharpToolboxService toolboxService = Program.ServiceManager.Get<IFlowSharpToolboxService>();

            // We don't display the toolbox, but we need a container.
            Panel pnlToolbox = new Panel();
            pnlToolbox.Visible = false;
            Controls.Add(pnlToolbox);

            toolboxService.CreateToolbox(pnlToolbox);
            toolboxService.InitializeToolbox();

            var mouseController = Program.ServiceManager.Get<IFlowSharpMouseControllerService>();
            mouseController.Initialize(canvasService.ActiveController);
        }

        protected void InitializeDhts()
        {
            dhts = new List<Dht>();
            dhtPos = new List<Rectangle>();

            NUM_DHT.ForEach((n) =>
            {
                IProtocol protocol = new VirtualProtocol();
                Dht dht = new Dht(ID.RandomID, protocol, () => new VirtualStorage(), new Router());
                ((VirtualProtocol)protocol).Node = dht.Node;
                dhts.Add(dht);
                dhtPos.Add(new Rectangle(XOFFSET + rnd.Next(-JITTER, JITTER) + (n % ITEMS_PER_ROW) * XSPACING, YOFFSET + rnd.Next(-JITTER, JITTER) + (n / ITEMS_PER_ROW) * YSPACING, SIZE, SIZE));
            });
        }

        protected void InitializeKnownPeers()
        {
            knownPeers = new List<Dht>();
            List<Dht> workingList = new List<Dht>(dhts);

            NUM_KNOWN_PEERS.ForEach(() =>
            {
                Dht knownPeer = workingList[rnd.Next(workingList.Count)];
                knownPeers.Add(knownPeer);
                workingList.Remove(knownPeer);
            });
        }

        private void OnShown(object sender, EventArgs e)
        {
            WebSocketHelpers.ClearCanvas();
            DrawDhts();
        }

        protected void DrawDhts()
        {
            dhtPos.ForEachWithIndex((p, i) => WebSocketHelpers.DropShape("Ellipse", i.ToString(), p, knownPeers.Contains(dhts[i]) ? Color.Red : Color.White, ""));

            dhts.ForEachWithIndex((d, i) =>
            {
                d.Node.BucketList.Buckets.SelectMany(b => b.Contacts).ForEach(c =>
                  {
                      int idx = dhts.FindIndex(target => target.ID == c.ID);
                      Point c1 = dhtPos[i].Center();
                      Point c2 = dhtPos[idx].Center();
                      var rect = new Rectangle(dhtPos[i].Center(), new Size(Math.Abs(c1.X - c2.X), Math.Abs(c1.Y - c2.Y)));
                      WebSocketHelpers.DropShape("DiagonalConnector", "c" + i, rect);
                  });
            });
        }

        private void btnStep_Click(object sender, EventArgs e)
        {
            Dht dht = dhts[peerBootrappingIdx];
            var peerList = knownPeers.ExceptBy(dht, c => c.ID).ToList();
            Dht bootstrapWith = peerList[rnd.Next(knownPeers.Count)];
            dht.Bootstrap(bootstrapWith.Contact);

            WebSocketHelpers.ClearCanvas();
            DrawDhts();

            ++peerBootrappingIdx;
        }
    }
}
