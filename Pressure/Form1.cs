using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Pressure
{
    public partial class Form1 : Form
    {
        const int W = 101, H = 101, D = 101, Scaler = 2;
        public Form1()
        {
            InitializeComponent();

            panel1.Paint += new PaintEventHandler(panel1_Paint);
            this.DoubleBuffered = true;
        }

        void panel1_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                double rPc = (double)numericUpDown2.Value / 100000;
                double rPc2 = (double)numericUpDown3.Value / 100000;
                double bPc = (double)numericUpDown5.Value / 100000;
                double bPc2 = (double)numericUpDown4.Value / 100000;

                e.Graphics.FillRectangle(Brushes.Black, panel1.DisplayRectangle);
                e.Graphics.DrawRectangle(Pens.DarkGreen, Scaler * W / 2, 0, Scaler * W, Scaler * H);
                e.Graphics.DrawLine(Pens.DarkGreen, Scaler * W / 2, 0, 0, Scaler * H / 2);
                e.Graphics.DrawLine(Pens.DarkGreen, Scaler * W / 2, Scaler * H, 0, Scaler * 3 * H / 2);

                double rDrawMin = curMin + (curMax - curMin) * rPc;
                double rDrawMax = curMin + (curMax - curMin) * rPc2;
                double bDrawMin = curMin + (curMax - curMin) * bPc;
                double bDrawMax = curMin + (curMax - curMin) * bPc2;
                
                double[, ,] curG = volumes[cur];
                for (int z = D - 1; z > 0; z--)
                {
                    double zz = (double)z / 2;
                    for (int x = 0; x < W; x++)
                        for (int y = 0; y < H; y++)
                        {
                            double curV = (curG[x, y, z] + curG[x, y, z - 1]) / 2;
                            bool drawRed = !((curV < rDrawMin) || (curV > rDrawMax));
                            bool drawBlue = !((curV < bDrawMin) || (curV > bDrawMax));

                            if (!drawRed && !drawBlue)
                                continue;
                            int red = drawRed ? (int)(128 + 127 * (curV - rDrawMin) / (rDrawMax - rDrawMin)) : 0;
                            int blue = drawBlue ? (int)(255 - 127 * (curV - bDrawMin) / (bDrawMax - bDrawMin)) : 0;

                            Color c = Color.FromArgb(red, 0, blue);
                            Brush b = new SolidBrush(c);
                            e.Graphics.FillRectangle(b, (int)(Scaler * (x + zz)), (int)(Scaler * (y - zz + (double)D / 2)), Scaler, Scaler);
                        }
                }

                e.Graphics.DrawRectangle(Pens.LightGreen, 0, Scaler * H / 2, Scaler * W, Scaler * H);
                e.Graphics.DrawLine(Pens.Green, Scaler * 3 * W / 2, 0, Scaler * W, Scaler * H / 2);
                e.Graphics.DrawLine(Pens.Green, Scaler * 3 * W / 2, Scaler * H, Scaler * W, Scaler * 3 * H / 2);
            }
            catch (Exception ex)
            {
                int xxxx = 4;
            }
        }

        double[][, ,] volumes = new double[][, ,] { new double[W + 2, H + 2, D + 2], new double[W + 2, H + 2, D + 2] };
        double[][, ,] vTemp = new double[][, ,] { new double[W + 1, H, D], new double[W, H + 1, D], new double[W, H, D + 1] };
        int cur = 0, nstep = 0;
        double curMin = 10000, curMax = 10010, curTot = 10 + 10000 * (double)(W * H * D);

        void init()
        {
            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                    for (int z = 0; z < D; z++)
                        volumes[0][x, y, z] = curMin;
            volumes[0][W / 2 + 1, H / 2 + 1, D / 2 + 1] = curMax;
        }

        class Index3D
        {
            public int x;
            public int y;
            public int z;
        }

        class Enumerable3D : IEnumerable<Index3D>
        {
            private int minX, maxX, minY, maxY, minZ, maxZ;

            public Enumerable3D(int x1, int x2, int y1, int y2, int z1, int z2)
            {
                minX = x1;
                maxX = x2;
                minY = y1;
                maxY = y2;
                minZ = z1;
                maxZ = z2;
            }

            public IEnumerator<Index3D> GetEnumerator()
            {
                for (int x = minX; x <= maxX; x++)
                    for (int y = minY; y <= maxY; y++)
                        for (int z = minZ; z <= maxZ; z++)
                            yield return new Index3D() { x = x, y = y, z = z };
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
        
        void step()
        {
            int next = 1 - cur;
            double[, ,] curG = volumes[cur];

            var e = new Enumerable3D(0, W - 2, 0, H - 1, 0, D - 1);
            Parallel.ForEach(e, (i) =>
            {
                int x = i.x, y = i.y, z = i.z;

                double curV1 = curG[x, y, z];
                double curV2 = curG[x + 1, y, z];
                vTemp[0][x + 1, y, z] = curV2 - curV1;
            });
            e = new Enumerable3D(0, W - 1, 0, H - 2, 0, D - 1);
            Parallel.ForEach(e, (i) =>
            {
                int x = i.x, y = i.y, z = i.z;

                double curV1 = curG[x, y, z];
                double curV2 = curG[x, y + 1, z];
                vTemp[1][x, y + 1, z] = curV2 - curV1;
            });
            e = new Enumerable3D(0, W - 1, 0, H - 1, 0, D - 2);
            Parallel.ForEach(e, (i) =>
            {
                int x = i.x, y = i.y, z = i.z;

                double curV1 = curG[x, y, z];
                double curV2 = curG[x, y, z + 1];
                vTemp[2][x, y, z + 1] = curV2 - curV1;
            });
            e = new Enumerable3D(0, W - 1, 0, H - 1, 0, D - 1);
            Parallel.ForEach(e, (i) =>
            {
                int x = i.x, y = i.y, z = i.z;

                double curV = curG[x, y, z];
                double dx = vTemp[0][x + 1, y, z] - vTemp[0][x, y, z];
                double dy = vTemp[1][x, y + 1, z] - vTemp[1][x, y, z];
                double dz = vTemp[2][x, y, z + 1] - vTemp[2][x, y, z];
                double result = volumes[next][x, y, z] = curV + (dx + dy + dz) / 6;
            });

            cur = next;
            nstep++;
        }

        void updateLabels()
        {
            label1.Text = string.Format("Min: {0}", curMin);
            label2.Text = string.Format("Max: {0}", curMax);
            label3.Text = string.Format("{0}", volumes[cur][26, 26, 26]);
            label4.Text = string.Format("Step {0}", nstep);
            label5.Text = string.Format("Tot: {0}", curTot);
        }

        Task t = null;
        CancellationTokenSource cts;
        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            numericUpDown1.Enabled = false;
            cts = new CancellationTokenSource();
            t = new Task(() =>
            {
                var token = cts.Token;
                int nSteps = (int)numericUpDown1.Value;
                for (int i = 0; i < nSteps && !token.IsCancellationRequested; i++)
                {
                    step();
                }
                getStats();
                Invoke(new Action(() => {
                    panel1.Invalidate();
                    updateLabels();
                    button1.Enabled = true;
                    numericUpDown1.Enabled = true;
                    t = null;
                }));
            }, cts.Token);
            t.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            init();
            updateLabels();
            panel1.Invalidate();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            cts.Cancel();
            button1.Enabled = true;
            numericUpDown1.Enabled = true;
        }

        void getStats()
        {
            double min = double.MaxValue;
            double max = 0;
            double tot = 0;
            var e = new Enumerable3D(0, W - 1, 0, H - 1, 0, D - 1);
            var curG = volumes[cur];
            foreach (var i in e)
            {
                int x = i.x, y = i.y, z = i.z;
                double curV = curG[x, y, z];
                if (curV < min)
                    min = curV;
                else if (curV > max)
                    max = curV;
                tot += curV;
            }

            curMin = min;
            curMax = max;
            curTot = tot;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            panel1.Invalidate();
        }
    }
}
