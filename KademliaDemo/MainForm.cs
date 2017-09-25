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
        protected int peerBootstrappingIdx = 0;
        protected List<Peer2Peer> connections;


        // 60 peer network:
        //protected const int NUM_DHT = 60;
        //protected const int ITEMS_PER_ROW = 10;
        //protected const int XOFFSET = 30;
        //protected const int YOFFSET = 30;
        //protected const int SIZE = 30;
        //protected const int XSPACING = 60;
        //protected const int YSPACING = 80;
        //protected const int JITTER = 15;
        //protected const int NUM_KNOWN_PEERS = 5;

        // 25 peer network:
        protected const int NUM_DHT = 25;
        protected const int ITEMS_PER_ROW = 5;
        protected const int XOFFSET = 30;
        protected const int YOFFSET = 30;
        protected const int SIZE = 30;
        protected const int XSPACING = 100;
        protected const int YSPACING = 100;
        protected const int JITTER = 15;
        protected const int NUM_KNOWN_PEERS = 3;

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
            connections = new List<Peer2Peer>();
            dhtPos.ForEachWithIndex((p, i) => WebSocketHelpers.DropShape("Ellipse", i.ToString(), p, knownPeers.Contains(dhts[i]) ? Color.Red : Color.Green, ""));

            dhts.ForEachWithIndex((d, i) =>
            {
                d.Node.BucketList.Buckets.SelectMany(b => b.Contacts).ForEach(c =>
                  {
                      int idx = dhts.FindIndex(target => target.ID == c.ID);
                      var otherDir = new Peer2Peer() { idx1 = idx, idx2 = i };

                      // Don't draw connector going back (idx -> i) because this is a redundant draw.  Speeds things up a little.
                      if (!connections.Contains(otherDir))
                      {
                          Point c1 = dhtPos[i].Center();
                          Point c2 = dhtPos[idx].Center();
                          WebSocketHelpers.DropConnector("DiagonalConnector", "c" + i, c1.X, c1.Y, c2.X, c2.Y, Color.Gray);
                          connections.Add(new Peer2Peer() { idx1 = i, idx2 = idx });
                      }
                  });
            });
        }

        protected void BootstrapWithAPeer(int peerBootstrappingIdx)
        {
            Dht dht = dhts[peerBootstrappingIdx];
            var peerList = knownPeers.ExceptBy(dht, c => c.ID).ToList();
            Dht bootstrapWith = peerList[rnd.Next(peerList.Count)];
            dht.Bootstrap(bootstrapWith.Contact);
        }

        protected void EnableBucketRefresh()
        {
            btnStep.Enabled = false;
            btnRun.Enabled = false;
            btnBucketRefresh.Enabled = true;
        }

        private void btnStep_Click(object sender, EventArgs e)
        {
            if (peerBootstrappingIdx < dhts.Count)
            {
                BootstrapWithAPeer(peerBootstrappingIdx);
                ++peerBootstrappingIdx;
            }
            else
            {
                EnableBucketRefresh();
            }
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            Enumerable.Range(peerBootstrappingIdx, dhts.Count - peerBootstrappingIdx).
                AsParallel().
                ForEach(n =>
            {
                BootstrapWithAPeer(n);
            });

            WebSocketHelpers.ClearCanvas();
            DrawDhts();
            EnableBucketRefresh();
        }

        /// <summary>
        /// Manually refresh all buckets and draw the new connections in purple.
        /// </summary>
        private void btnBucketRefresh_Click(object sender, EventArgs e)
        {
            dhts.AsParallel().ForEach(d => d.PerformBucketRefresh());

            dhts.ForEachWithIndex((d, i) =>
            {
                d.Node.BucketList.Buckets.SelectMany(b => b.Contacts).ForEach(c =>
                {
                    int idx = dhts.FindIndex(target => target.ID == c.ID);
                    var current = new Peer2Peer() { idx1 = i, idx2 = idx };
                    var otherDir = new Peer2Peer() { idx1 = idx, idx2 = i };

                    // Don't draw connector going back (idx -> i) because this is a redundant draw.  Speeds things up a little.
                    if (!connections.Contains(otherDir) && !connections.Contains(current) )
                    {
                        Point c1 = dhtPos[i].Center();
                        Point c2 = dhtPos[idx].Center();
                        WebSocketHelpers.DropConnector("DiagonalConnector", "c" + i, c1.X, c1.Y, c2.X, c2.Y, Color.Purple);
                        connections.Add(new Peer2Peer() { idx1 = i, idx2 = idx });
                    }
                });
            });
        }
    }

    public struct Peer2Peer
    {
        public int idx1;
        public int idx2;
    }
}
