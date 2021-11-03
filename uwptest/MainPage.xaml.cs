using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.Threading.Tasks;
using System.Numerics;
using Windows.UI;
//using System.Xml.Linq;
using System.Xml;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace uwptest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    //classes for spritesheet and shit
    public class Spritesheet
    {
        public string Path { get; }
        public int Id { get; }
        public bool IsLoaded { get; set; }
        public CanvasBitmap Bitmap { get; set; }
        //       public string Path;
        //       public int Id;
        public Spritesheet(string path, int id)
        {
            Path = path;
            Id = id;
        }
        public async void LoadCanvas(string globalpath, Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl drawdevice)
        {
            StorageFile file = null;
            try
            {
                string winpath = Path.Replace("/","\\");
                string finalpath = System.IO.Path.GetDirectoryName(globalpath) + "\\" + winpath;
                Debug.WriteLine(finalpath);
                file = await StorageFile.GetFileFromPathAsync(finalpath);
                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    Bitmap = await CanvasBitmap.LoadAsync(drawdevice, stream);
                    IsLoaded = true;
                }
            }
            catch { Debug.WriteLine("catch worked!"); IsLoaded = false; }
            Debug.WriteLine("is spritesheet loaded? " + IsLoaded.ToString());
        }
    }

    public class ANM2LayerFrame
    {
        public float XPosition { get; set; }
        public float YPosition { get; set; }
        public double XPivot { get; set; }
        public double YPivot { get; set; }
        public int Delay { get; set; }
        public int StartFrame { get; set; }
        public bool Interpolated { get; set; }
        public bool Visible { get; set; }
        public float Rotation { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double XCrop { get; set; }
        public double YCrop { get; set; }
        public float XScale { get; set; }
        public float YScale { get; set; }
        public float AlphaTint { get; set; }



        public ANM2LayerFrame(float xpos, float ypos, int startframe, int delay, int width, int height, double xpivot, double ypivot, double xcrop, double ycrop, float xscale,float yscale,float rotation,bool interpolate, bool visible,float alphatint)
        {
            XPosition = xpos;
            YPosition = ypos;
            StartFrame = startframe;
            Delay = delay;
            Width = width;
            Height = height;
            XPivot = xpivot;
            YPivot = ypivot;
            XCrop = xcrop;
            YCrop = ycrop;
            XScale = xscale;
            YScale = yscale;
            Rotation = rotation;
            Interpolated = interpolate;
            Visible = visible;
            AlphaTint = alphatint;
        }
    }


    public class ANM2Animation
    {
        public string Name { get; set; }
        public int FrameNum { get; set; }
        public bool Loop { get; set; }
        public List<ANM2LayerAnimation> AnimLayerList { get; set; }
        public ANM2Animation(string name,int framenum, bool loop)
        {
            Name = name;
            Loop = loop;
            FrameNum = framenum;
            AnimLayerList = new List<ANM2LayerAnimation> { };
        }
    }

    public class ANM2Layer
    {
        public string Name { get; }
        public int Id { get; }
        public int SpritesheetId { get; }
        //       public string Path;
        //       public int Id;
        public ANM2Layer(string name, int id, int spritesheetid)
        {
            Name = name;
            Id = id;
            SpritesheetId = spritesheetid;
        }
    }

    public class ANM2LayerAnimation
    {
        public int LayerId { get; set; }
        public ANM2Layer Layer { get; set; }
        public bool Visible { get; set; }
        public List<ANM2LayerFrame> FrameList { get; set; }
        public int CurrentIndex { get; set; }
        public ANM2LayerAnimation(int layerid,bool visible,List<ANM2Layer> layerlist)
        {
            LayerId = layerid;
            Visible = visible;
            Layer = layerlist[layerid];
            FrameList = new List<ANM2LayerFrame> { };
            CurrentIndex = 0;
        }
    }

    public class ANM2RootAnimation
    {
        public bool Visible { get; set; }
        public List<ANM2LayerFrame> FrameList { get; set; }
    }

    public sealed partial class MainPage : Page
    {

        public List<Spritesheet> spritesheetlist = new List<Spritesheet>() { };
        public List<ANM2Layer> layerlist = new List<ANM2Layer>() { };
        public List<ANM2Animation> animlist = new List<ANM2Animation>() { };
        public List<ANM2LayerAnimation> layeranimlist = new List<ANM2LayerAnimation>() { };
        public string defaultanimationname;
        public string anm2path;
        public int defaultanimindex;
        public int animindextoplay;
        float renderscale = 1.0f;
        float targetrenderscale = 1.0f;
        public bool IsANM2LoadFinished = false;

        public MainPage()
        {
            this.InitializeComponent();
            this.SpritesheetList.ItemsSource = spritesheetlist;
            this.LayerList.ItemsSource = layerlist;
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
        }


        public CanvasBitmap plum;
        public Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl drawingdevice;

        private async void MenuFlyoutItem_Click_OpenAsync(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.FileTypeFilter.Add(".anm2");
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null & file.Path != null)
            {
                Debug.WriteLine(System.IO.Path.GetDirectoryName(file.Path));
            }
        }

        public XmlDocument anm2contents = new XmlDocument();

        public float framecounter = 0;
        public float totalframecount = 0;
        public bool IsPlaying = false;
        public float PlaybackSpeed = (20.0f / 60.0f);
        public bool ResetFrames = false;

        private async void MenuFlyoutItem_Click_ReadANM2(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.FileTypeFilter.Add(".anm2");
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                IsANM2LoadFinished = false;
                anm2path = file.Path;
                spritesheetlist = new List<Spritesheet>() { };
                layerlist = new List<ANM2Layer>() { };
                layeranimlist = new List<ANM2LayerAnimation>() { };
                animlist = new List<ANM2Animation>() { };
                Debug.WriteLine(file.Path);
                TitleBarText.Text = "Dory (dev) - " + file.Path;
                defaultanimindex = -1;
                using (var stream = await file.OpenStreamForReadAsync())
                {

                    anm2contents.Load(stream);
                    XmlElement docroot = anm2contents.DocumentElement;
                    foreach (XmlNode xnode in docroot)
                    {
                        if (xnode.Name == "Content")
                        {
                            foreach (XmlNode contentnode in xnode)
                            {
                                if (contentnode.Name == "Spritesheets")
                                {
                                    foreach (XmlNode spritesheetnode in contentnode)
                                    {
                                        XmlAttributeCollection attrColl = spritesheetnode.Attributes;
                                        string spritesheetpath = "";
                                        int spritesheetid = -1;
                                        for (int i = 0; i < attrColl.Count; i++)
                                        {
                                            XmlAttribute attr = attrColl[i];
                                            if (attr.Name == "Id")
                                            {
                                                spritesheetid = Int32.Parse(attr.Value);
                                            }
                                            if (attr.Name == "Path")
                                            {
                                                spritesheetpath = attr.Value;
                                            }
                                        }
                                        Spritesheet output = new Spritesheet(spritesheetpath, spritesheetid);
                                        output.LoadCanvas(anm2path, drawingdevice);
                                        spritesheetlist.Insert(spritesheetid, output);
                                    }
                                }
                                if (contentnode.Name == "Layers")
                                {
                                    foreach (XmlNode layernode in contentnode)
                                    {
                                        XmlAttributeCollection attrColl = layernode.Attributes;
                                        string layername = "";
                                        int layerid = -1;
                                        int layerspritesheetid = -1;
                                        for (int i = 0; i < attrColl.Count; i++)
                                        {
                                            XmlAttribute attr = attrColl[i];
                                            if (attr.Name == "Id")
                                            {
                                                layerid = Int32.Parse(attr.Value);
                                            }
                                            if (attr.Name == "SpritesheetId")
                                            {
                                                layerspritesheetid = Int32.Parse(attr.Value);
                                                Debug.WriteLine("Spritesheet id: " + Convert.ToString(attr.Value));
                                            }
                                            if (attr.Name == "Name")
                                            {
                                                layername = attr.Value;
                                            }
                                        }
                                        ANM2Layer output = new ANM2Layer(layername, layerid, layerspritesheetid);
                                        layerlist.Add(output);
                                    }
                                }
                            }
                        }
                        if (xnode.Name == "Animations")
                        {
                            XmlAttributeCollection defaultAnimAttr = xnode.Attributes;
                            for (int i = 0; i < defaultAnimAttr.Count; i++)
                            {
                                XmlAttribute attr = defaultAnimAttr[i];
                                if (attr.Name == "DefaultAnimation")
                                {
                                    defaultanimationname = attr.Value;
                                    Debug.WriteLine("Default anim name: " + defaultanimationname);
                                }
                            }
                            foreach (XmlNode animnode in xnode)
                            {
                                string AnimName = "";
                                int AnimFrameNum = 0;
                                bool AnimLoop = false;
                                if (animnode.Name == "Animation")
                                {
                                    XmlAttributeCollection animAttr = animnode.Attributes;
                                    for (int i = 0; i < animAttr.Count; i++)
                                    {
                                        XmlAttribute attr = animAttr[i];
                                        if (attr.Name == "Name")
                                        {
                                            AnimName = attr.Value;
                                            if (attr.Value == defaultanimationname)
                                            {
                                                defaultanimindex = animlist.Count;
                                            }
                                        }
                                        if (attr.Name == "Loop")
                                        {
                                            //                                            AnimLoop = attr.Value;
                                            if (attr.Value == "true")
                                            {
                                                AnimLoop = true;
                                            }
                                        }
                                        if (attr.Name == "FrameNum")
                                        {
                                            AnimFrameNum = Int32.Parse(attr.Value);
                                        }
                                    }
                                    ANM2Animation animation = new ANM2Animation(AnimName, AnimFrameNum, AnimLoop);
                                    Debug.WriteLine("Default anim name: " + defaultanimationname);
                                    Debug.WriteLine("Anim name: " + AnimName + ", count: " + defaultanimindex.ToString());
                                    foreach (XmlNode animxnode in animnode)
                                    {
                                        if (animxnode.Name == "LayerAnimations")
                                        {
                                            foreach (XmlNode animlayernode in animxnode)
                                            {
                                                if (animlayernode.Name == "LayerAnimation")
                                                {
                                                    XmlAttributeCollection animlayerattr = animlayernode.Attributes;
                                                    int animlayerid = 0;
                                                    for (int i = 0; i < animlayerattr.Count; i++)
                                                    {
                                                        XmlAttribute attr = animAttr[i];
                                                        if (attr.Name == "LayerId")
                                                        {
                                                            animlayerid = Convert.ToInt32(attr.Value);
                                                            Debug.WriteLine("Layer Id: " + Convert.ToString(animlayerid));
                                                        }
                                                    }
                                                    ANM2LayerAnimation layeranim = new ANM2LayerAnimation(Int32.Parse(animlayerattr["LayerId"].Value), Boolean.Parse(animlayerattr["Visible"].Value), layerlist);
                                                    int startframe = 0;
                                                    foreach (XmlNode framenode in animlayernode)
                                                    {
                                                        XmlAttributeCollection frameAttr = framenode.Attributes;
                                                        ANM2LayerFrame frame = new ANM2LayerFrame(Single.Parse(frameAttr["XPosition"].Value), Single.Parse(frameAttr["YPosition"].Value), startframe, Int32.Parse(frameAttr["Delay"].Value), Int32.Parse(frameAttr["Width"].Value), Int32.Parse(frameAttr["Height"].Value), Double.Parse(frameAttr["XPivot"].Value), Double.Parse(frameAttr["YPivot"].Value), Double.Parse(frameAttr["XCrop"].Value), Double.Parse(frameAttr["YCrop"].Value), Single.Parse(frameAttr["XScale"].Value), Single.Parse(frameAttr["YScale"].Value), Single.Parse(frameAttr["Rotation"].Value), Boolean.Parse(frameAttr["Interpolated"].Value), Boolean.Parse(frameAttr["Visible"].Value), Single.Parse(frameAttr["AlphaTint"].Value));
                                                        //                                                        layeranim.FrameList.Add();
                                                        layeranim.FrameList.Add(frame);
                                                        startframe += Int32.Parse(frameAttr["Delay"].Value);
                                                    }
                                                    animation.AnimLayerList.Add(layeranim);
                                                }
                                            }
                                        }
                                    }
                                    animlist.Add(animation);
                                }

                            }

                        }
                    }
                }
                //                SpritesheetList.Items.Refresh();
                SpritesheetList.ItemsSource = null;
                SpritesheetList.ItemsSource = spritesheetlist;
                LayerList.ItemsSource = null;
                LayerList.ItemsSource = layerlist;
                AnimationList.ItemsSource = null;
                AnimationList.ItemsSource = animlist;
                AnimationList.SelectedIndex = defaultanimindex;
                animindextoplay = defaultanimindex;
                IsANM2LoadFinished = true;
                totalframecount = animlist[animindextoplay].FrameNum;
                framecounter = 0;
                IsPlaying = false;
                PausePlayBtn.Content = "";
            }
        }

        private async void MenuFlyoutItem_Click_OpenPlumAsync(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.FileTypeFilter.Add(".png");
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null && file.Path != null)
            {
                Debug.WriteLine(file.Path);
                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    plum = await CanvasBitmap.LoadAsync(drawingdevice, stream);
                }
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            this.anm2canvas.RemoveFromVisualTree();
            this.anm2canvas = null;
        }

        private void canvas_CreateResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
        }
        Vector2 canvasoffset = new Vector2(250f, 250f);
        Vector2 canvasvelocity = new Vector2(0, 0);
        Vector2 previousposition = new Vector2(0, 0);
        Vector2 applyposition = new Vector2(0, 0);
        bool IsMouseBtnHeld = false;
        private async void canvas_Update(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedUpdateEventArgs args)
        {
            if (targetrenderscale != renderscale)
            {
                if (targetrenderscale > renderscale)
                {
                    renderscale += 0.05f;
                }
                if (targetrenderscale < renderscale)
                {
                    renderscale -= 0.05f;
                }
            }
            if (IsPlaying == true)
            {
                framecounter += PlaybackSpeed;
                if (framecounter >= totalframecount)
                {
                    framecounter = 0;
                    ResetFrames = true;
                }
            }
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                //code
                FrameCountDisplay.Text = "Frame: " + Convert.ToString(framecounter) + "/" + Convert.ToString(totalframecount);
            });

        }

        private void canvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args)
        {
            drawingdevice = sender;
            if (plum != null)
            {
                args.DrawingSession.DrawText("Renderer Placeholder", 100, 120, Windows.UI.Colors.White);
                var transform = Matrix3x2.CreateRotation(0, new Vector2(0, 0)) *
                                Matrix3x2.CreateScale(2.0f, 2.0f, new Vector2(0, 0)) *
                                Matrix3x2.CreateTranslation(new Vector2(0, 0));
                args.DrawingSession.DrawImage(plum, new Vector2(125, 0), new Rect(0, 0, 192, 192), 1, 0, new Matrix4x4(transform));
            }
            Color AxisColor = Color.FromArgb(191, 255, 0, 0);
            args.DrawingSession.DrawLine(new Vector2(-999 + canvasoffset.X, canvasoffset.Y), new Vector2(999 + canvasoffset.X, canvasoffset.Y), AxisColor);
            args.DrawingSession.DrawLine(new Vector2(canvasoffset.X, -999 + canvasoffset.Y), new Vector2(canvasoffset.X, 999 + canvasoffset.Y), AxisColor);
            if (IsANM2LoadFinished == true)
            {
                ANM2Animation currentanim = animlist[animindextoplay];
                List<ANM2LayerAnimation> layeranimlist = currentanim.AnimLayerList;
                foreach (ANM2LayerAnimation layeranim in layeranimlist)
                {
                    ANM2LayerFrame layerframe;
                    if (layeranim.FrameList != null && layeranim.FrameList.Count > 0)
                    {
                        try
                        {
                            canvasoffset = canvasoffset + canvasvelocity;
                            canvasvelocity.X = canvasvelocity.X * 0.8f;
                            canvasvelocity.Y = canvasvelocity.Y * 0.8f;
                            layerframe = layeranim.FrameList[layeranim.CurrentIndex];
                            bool CanChangeFrame = layeranim.CurrentIndex + 1 < layeranim.FrameList.Count;
                            //                     Debug.WriteLine(layeranim.LayerId);
                            float finalscalex = layerframe.XScale;
                            float finalscaley = layerframe.YScale;
                            float finalpositionx = layerframe.XPosition;
                            float finalpositiony = layerframe.YPosition;
                            float finalrotation = layerframe.Rotation;
                            float finalalpha = layerframe.AlphaTint;
                            float flipx = 1;
                            float flipy = 1;
                            CanvasBitmap spritesheettodraw;
                            if (layerframe.Interpolated == true && CanChangeFrame)
                            {
                                ANM2LayerFrame nextframe = layeranim.FrameList[layeranim.CurrentIndex + 1];
                                float lerppercent = (framecounter - layerframe.StartFrame) / layerframe.Delay;
                                finalscalex = layerframe.XScale * (1 - lerppercent) + nextframe.XScale * lerppercent;
                                finalscaley = layerframe.YScale * (1 - lerppercent) + nextframe.YScale * lerppercent;
                                finalpositionx = layerframe.XPosition * (1 - lerppercent) + nextframe.XPosition * lerppercent;
                                finalpositiony = layerframe.YPosition * (1 - lerppercent) + nextframe.YPosition * lerppercent;
                                finalrotation = layerframe.Rotation * (1 - lerppercent) + nextframe.Rotation * lerppercent;
                                finalalpha = layerframe.AlphaTint * (1 - lerppercent) + nextframe.AlphaTint * lerppercent;
                            }
                            spritesheettodraw = spritesheetlist[layeranim.Layer.SpritesheetId].Bitmap;
                            if (finalscalex < 0)
                            {
                                finalscalex = finalscalex * -1;
                                flipx = -1;
                            }
                            if (finalscaley < 0)
                            {
                                finalscaley = finalscaley * -1;
                                flipy = -1;
                            }
                            Vector2 testvec = new Vector2(Convert.ToSingle(1 * layerframe.XPivot), Convert.ToSingle(1 * layerframe.YPivot));
                            Vector2 rotatedpivotvec = new Vector2(Convert.ToSingle(1 * layerframe.XPivot), Convert.ToSingle(1 * layerframe.YPivot));
                            rotatedpivotvec = Vector2.Transform(rotatedpivotvec, Matrix3x2.CreateRotation(((180 - finalrotation) * (Convert.ToSingle(Math.PI) / 180))));
                            var transform = Matrix3x2.CreateRotation(finalrotation * (Convert.ToSingle(Math.PI) / 180), testvec) *
                                            Matrix3x2.CreateScale(flipx, flipy, new Vector2(0, 0)) *
                                            //                                            Matrix3x2.CreateTranslation(new Vector2(Convert.ToSingle(layerframe.XPivot * -1.0f), (Convert.ToSingle(layerframe.YPivot*-1.0f))));
                                            Matrix3x2.CreateTranslation(new Vector2((canvasoffset.X + finalpositionx * renderscale), (canvasoffset.Y + finalpositiony * renderscale)));       //this is obviously wrong!!!
                            //                            args.DrawingSession.DrawImage(spritesheettodraw, new Rect((canvasoffset.X + finalpositionx * 1.0f), (canvasoffset.Y + finalpositiony * 1.0f), layerframe.Width * 0.01f * finalscalex, layerframe.Height * 0.01f * finalscaley), new Rect(layerframe.XCrop, layerframe.YCrop, layerframe.Width, layerframe.Height), 1, 0, new Matrix4x4(transform));
                            if (layeranim.Visible == true && layerframe.Visible == true)
                            {
                                args.DrawingSession.DrawImage(spritesheettodraw, new Rect(rotatedpivotvec.X * finalscalex * 0.01f * renderscale, rotatedpivotvec.Y * finalscaley * 0.01f * renderscale, layerframe.Width * 0.01f * finalscalex * renderscale, layerframe.Height * 0.01f * finalscaley * renderscale), new Rect(layerframe.XCrop, layerframe.YCrop, layerframe.Width, layerframe.Height), finalalpha / 255, 0, new Matrix4x4(transform));
                            }
                            else
                            {
                                Debug.WriteLine(Convert.ToString(layeranim.Visible) + " " + Convert.ToString(layerframe.Visible));
                            }
                            if (ResetFrames == true)
                            {
                                layeranim.CurrentIndex = 0;
                                layerframe = layeranim.FrameList[layeranim.CurrentIndex];
                            }
                            else
                            {
                                if (framecounter > layerframe.StartFrame + layerframe.Delay && CanChangeFrame == true)
                                {
                                    layeranim.CurrentIndex += 1;
                                    layerframe = layeranim.FrameList[layeranim.CurrentIndex];
                                }
                            }
                        }
                        catch { }
                    }
                }
                ResetFrames = false;
            }
        }


        private void MenuFlyoutItem_Click_RemovePlumAsync(object sender, RoutedEventArgs e)
        {
            plum = null;
        }


        private void anm2canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            Windows.UI.Xaml.Input.Pointer ptr = e.Pointer;
            if (ptr.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                Windows.UI.Input.PointerPoint ptrPt = e.GetCurrentPoint(anm2canvas);
                if (ptrPt.Properties.IsLeftButtonPressed)
                {
                    Point pos = ptrPt.Position;
                    if (IsMouseBtnHeld != true)
                    {
                        IsMouseBtnHeld = true;
                        previousposition.X = Convert.ToSingle(pos.X);
                        previousposition.Y = Convert.ToSingle(pos.Y);
                    }

                    applyposition.X = (Convert.ToSingle(pos.X) - previousposition.X) * 0.3f;
                    applyposition.Y = (Convert.ToSingle(pos.Y) - previousposition.Y) * 0.3f;
                    canvasvelocity = canvasvelocity + applyposition;
                    previousposition.X = Convert.ToSingle(pos.X);
                    previousposition.Y = Convert.ToSingle(pos.Y);
                }
                else
                {
                    if (ptrPt.Properties.IsRightButtonPressed)
                    {
                        canvasoffset = new Vector2(250f, 250f);
                        canvasvelocity = new Vector2(0, 0);
                        previousposition = new Vector2(0, 0);
                        applyposition = new Vector2(0, 0);
                    }
                    if (IsMouseBtnHeld == true)
                    {
                        IsMouseBtnHeld = false;
                        previousposition = new Vector2(0, 0);
                    }
                }
            }
            e.Handled = true;
        }

        private void AnimationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsANM2LoadFinished == true)
            {
                animindextoplay = AnimationList.SelectedIndex;
                totalframecount = animlist[animindextoplay].FrameNum;
                framecounter = 0;
            }
        }

        private void PausePlayBtn_Click(object sender, RoutedEventArgs e)
        {
            if (IsPlaying == true)
            {
                IsPlaying = false;
                PausePlayBtn.Content = "";
            }
            else
            {
                IsPlaying = true;
                PausePlayBtn.Content = "";
            }
        }

        private async void MenuFlyoutItem_ClickFS(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess"));
        }

        private void anm2canvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            Windows.UI.Input.PointerPoint ptrPt = e.GetCurrentPoint(anm2canvas);
            int MouseWheelDelta = ptrPt.Properties.MouseWheelDelta;
            if (MouseWheelDelta > 0)
            {
                targetrenderscale += 0.2f;
            }
            if (MouseWheelDelta < 0)
            {
                targetrenderscale -= 0.2f;
            }
        }
    }
}