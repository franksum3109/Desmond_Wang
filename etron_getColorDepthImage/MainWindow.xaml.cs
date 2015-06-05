using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;


namespace etron_hello
{
    /// <summary>
    /// MainWindow.xaml 
    /// </summary>
    public partial class MainWindow : Window
    {
        /*
         * Import methods from "Etron.dll"
         */

        [DllImport(@"C:\Users\Desmond\Documents\GitHub\Desmond\etron_getColorDepthImage\obj\Debug\EtronDI.dll", CallingConvention = CallingConvention.Cdecl)]
        unsafe static extern void EtronDI_Init( void** ppHandleEtronDI);

        [DllImport(@"C:\Users\Desmond\Documents\GitHub\Desmond\etron_getColorDepthImage\obj\Debug\EtronDI.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void EtronDI_Release();

        [DllImport(@"C:\Users\Desmond\Documents\GitHub\Desmond\etron_getColorDepthImage\obj\Debug\EtronDI.dll", CallingConvention = CallingConvention.Cdecl)]
        unsafe static extern bool EtronDI_FindDevice(void* ppHandleEtronDI);

        [DllImport(@"C:\Users\Desmond\Documents\GitHub\Desmond\etron_getColorDepthImage\obj\Debug\EtronDI.dll", CallingConvention = CallingConvention.Cdecl)]
        unsafe static extern bool EtronDI_OpenDevice(void* pHandleEtronDI, int nWidth, int nHeight, bool bImageL, bool bDepth, void* phWndNotice = null);

        [DllImport(@"C:\Users\Desmond\Documents\GitHub\Desmond\etron_getColorDepthImage\obj\Debug\EtronDI.dll", CallingConvention = CallingConvention.Cdecl)]
        unsafe static extern int EtronDI_GetDeviceResolutionList(void* pHandleEtronDI, int nMaxCount0, ETRONDI_STREAM_INFO[] pStreamInfo0, int nMaxCount1, ETRONDI_STREAM_INFO[] pStreamInfo1);

        [DllImport(@"C:\Users\Desmond\Documents\GitHub\Desmond\etron_getColorDepthImage\obj\Debug\EtronDI.dll", CallingConvention = CallingConvention.Cdecl)]
        unsafe static extern bool EtronDI_OpenDevice2(void* pHandleEtronDI, int nWidth, int nHeight, bool bImageL, int nWidth2, int nHeight2, void* phWndNotice = null);

        [DllImport(@"C:\Users\Desmond\Documents\GitHub\Desmond\etron_getColorDepthImage\obj\Debug\EtronDI.dll", CallingConvention = CallingConvention.Cdecl)]
        unsafe static extern int EtronDI_GetDeviceNumber(void* m_hEtronDI);

        [DllImport(@"C:\Users\Desmond\Documents\GitHub\Desmond\etron_getColorDepthImage\obj\Debug\EtronDI.dll", CallingConvention = CallingConvention.Cdecl)]
        unsafe static extern int EtronDI_SelectDevice(void* m_hEtronDI, int dev_index);

        [DllImport(@"C:\Users\Desmond\Documents\GitHub\Desmond\etron_getColorDepthImage\obj\Debug\EtronDI.dll", CallingConvention = CallingConvention.Cdecl)]
        unsafe static extern bool EtronDI_GetImage(void* pHandleEtronDI, byte[] pBuf, int[] pSerial = null);

        [DllImport(@"C:\Users\Desmond\Documents\GitHub\Desmond\etron_getColorDepthImage\obj\Debug\EtronDI.dll", CallingConvention = CallingConvention.Cdecl)]
        unsafe static extern bool EtronDI_Get2Image(void* pHandleEtronDI, byte[] pBuf, byte[] pdBuf, int[] pSerial = null, int[] pdSerial = null);

        /*
         * Define size of Image. (In pixels)
         * Note that other size might not be accepted by Etron board, and Etron board might fail to turn on.
         * Using EtronDI_OpenDevice, size of depth image must be set as the same size of color image.
         */
        const int deep_width = 640;
        const int deep_height = 480;
        const int color_width = 640;
        const int color_height = 480;

        //Buffer for the image data get from the device.
        //Note that color image size has to be multiplied by 2 due to the YUY2 form.
        byte[] colorPixels = new byte[color_width * color_height*2];
        byte[] depthPixels = new byte[deep_width * deep_height];

        //Image source (bit map) that is going to be shown.
        WriteableBitmap colorBitmap;
        WriteableBitmap depthBitmap;

        //Maximum image stream number.
        const int m_nStreamColorInfoCountMax = 64;
        const int m_nStreamDepthInfoCountMax = 16;

        //Generate a structure that defined exacly the same in EtronDI.dll.
        struct ETRONDI_STREAM_INFO { 
            public int nWidth;              // Stream Image Width 
            public int nHeight;             // Stream Image Width 
            public Boolean bFormatMJPG;     // 0 for YUY2; others for MJPG 
        };

        //Device controller.
        unsafe void* m_hEtronDI = null;

        public MainWindow()
        {
            unsafe
            {
                InitializeComponent();      //Initialize components.

                //Initial the device and return the device controller to m_hEtronDI.
                fixed (void** temp2 = &m_hEtronDI)
                {
                    EtronDI_Init(temp2);
                }


                if (EtronDI_FindDevice(m_hEtronDI))
                {
                    EtronDI_SelectDevice(m_hEtronDI, 1);

                    //To store resolution data. However, no data will be stored so far.
                    ETRONDI_STREAM_INFO[] m_StreamColorInfo = new ETRONDI_STREAM_INFO[64];
                    ETRONDI_STREAM_INFO[] m_StreamDepthInfo = new ETRONDI_STREAM_INFO[16];
                    int m_nStreamColorInfoCount;
                    int m_nStreamDepthInfoCount;
                    int nRef = 0;

                    if (nRef >= 0) 
                    {
                        //Select Etron board. Note that there might be a cam on laptop.
                        //So you might need to change the selection.
                        EtronDI_SelectDevice(m_hEtronDI, 1);
                        //There won't be any resolution saved so far. 
                        //However, we can determine if the Etron board is connected or not with this.
                        nRef = EtronDI_GetDeviceResolutionList(m_hEtronDI, m_nStreamColorInfoCountMax, m_StreamColorInfo, m_nStreamDepthInfoCountMax, m_StreamDepthInfo);
                        if (nRef >= 0)
                        {
                            m_nStreamColorInfoCount = (nRef >> 8) & 0xFF;
                            m_nStreamDepthInfoCount = (nRef) & 0xFF;

                            //Open Device with different function.
                            //EtronDI_OpenDevice2 cannot be correctly used so far.
                            //Here, we close the color image and enable the depth image.
                            EtronDI_OpenDevice(m_hEtronDI, deep_width, deep_height, false, true);
                           // EtronDI_OpenDevice2(m_hEtronDI, color_width, color_height, false, deep_width, deep_height);
                        }
                    }
                }
                else
                {
                    this.hello.Text = "Not found QQ";
                }
                 
                   
            }
        }

        //A method to get image data.
        private unsafe void get_data()
        {
            while(true)
            {
                try
                {
                    //Get image data from Etron board.
                    bool gett = EtronDI_GetImage(m_hEtronDI, depthPixels);
                 //   bool gett = EtronDI_Get2Image(m_hEtronDI, colorPixels, depthPixels);

                    if (gett)
                    {
                        //Update images.

                    //    updateColorImage();
                        updateDepthImage();
                    }
                }
                catch
                {
                }
            }
        }

         
        //Method to update image.
        public void updateColorImage()
        {
            if (!(colorBitmap.Dispatcher.CheckAccess()))
            {
                colorBitmap.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(updateColorImage));
            }
            else {
                //dss is a buffer we use to store RGB data.
                byte[] dss = new byte[color_width * color_height / 4 * 6];
                //Transform YUY2 form to RGB
                YUY2ToRGB(colorPixels, color_width * color_height, dss);

                //Map byte date to a image.
                Int32Rect ttemp = new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight);
                colorBitmap.WritePixels(
                    ttemp,
                    dss,
                    color_width * 3,        //width pixel x bytes per pixel
                    0);
            }
        }

        //Use mode number to filt some of the noise.
        int num_avg = 20;           //Number of sample each time we filt the noise.
        public void updateDepthImage()
        {
            if (!(depthBitmap.Dispatcher.CheckAccess()))
            {
                depthBitmap.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(updateDepthImage));
            }
            else
            {
                //dss is a buffer we use to depth image data.
                byte[] dss = new byte[deep_width * deep_height];

                for (int i = 0; i < deep_width * deep_height; i++)
                {
                    //Restrict the range that we want.
                    if (depthPixels[i] > 128 && depthPixels[i] < 256)
                    {
                        //Modify the remained byte to 0 - 255
                        dss[i] = (byte)((depthPixels[i] - 128)*2);
                    }
                    else
                    {
                        dss[i] = 0;
                    }
                }

                //Pick the mode and filt the noise
                for (int i = 0; i < deep_width * deep_height; i += num_avg)
                {
                    byte[] temppp = new byte[num_avg];
                    if ((i + num_avg) % deep_width < num_avg && (i + num_avg) % deep_width > 0)
                    {
                        i = ((i + num_avg) / deep_width) * deep_width - num_avg;
                        continue;
                    }
                    for (int j = 0; j < num_avg; j++)
                    {
                        temppp[j] = dss[i + j];
                    }
                    int most_num = PickMost(temppp);
                    for (int j = 0; j < num_avg; j++)
                    {
                        dss[i + j] = (byte)most_num;
                    }
                }
                
                // Print some depth data.
                string str = "";
                for (int i = 200; i < 1000; i++)
                {
                    str += depthPixels[i] + " ";
                }
                UI_Update(str, this.hello);

                //Slow down the update.
                Thread.Sleep(100);

                //Map byte date to a image.
                Int32Rect ttemp = new Int32Rect(0, 0, depthBitmap.PixelWidth, depthBitmap.PixelHeight);
                depthBitmap.WritePixels(
                    ttemp,
                    dss,
                    deep_width,        //width pixel x bytes per pixel
                    0);
            }
        }

        //Method to pick mode number in a window
        private int PickMost(byte[] input)
        {
            int max_num = 0;
            int max_index = 0;
            for (int i = 0; i < num_avg-1; i++)
            {
                int num = 0;
                for (int j = i; j < num_avg; j++)
                {
                    if (input[i] == input[j]) num++;
                }
                if (num > max_num)
                {
                    max_num = num;
                    max_index = i;
                }
            }
            return input[max_index];
        }

        //Update UI.
        private delegate void UIupdateCallback(string value, TextBlock ctl);
        private void UI_Update(string value, TextBlock ctl)
        {
            if (!ctl.Dispatcher.CheckAccess())
            {
                UIupdateCallback update = new UIupdateCallback(UI_Update);
                this.Dispatcher.Invoke(update, value, ctl);
            }
            else
            {
                if (value == "")
                    ctl.Text = "";
                else
                    ctl.Text = value;
            }
        }

        //Get image after button clicked.
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.state.Text = "btt down";
            this.hello.Text = "";

            ThreadPool.QueueUserWorkItem(o =>
            {
                get_data();
            });
            
        }

        //Initialize some data and assign the source of image in xaml.
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            colorBitmap = new WriteableBitmap(color_width, color_height, 150.0, 150.0, PixelFormats.Bgr24, null);
            depthBitmap = new WriteableBitmap(deep_width, deep_height, 150.0, 150.0, PixelFormats.Gray8, null);
            imm.Source = depthBitmap;   //colorBitmap;
            dimm.Source = depthBitmap;
        }

        //Transform YUY2 image to a RGB one
        unsafe void YUY2ToRGB(byte[] src, int len, byte[] dst)
        {
            int count = 0;
            for (int i = 0; i < len; i+=4){
                int Y = src[i];
                int U = src[i+1];
                int V = src[i+3];

                int y = Y -16;
                int u = U -128;
                int v = V -128;

                dst[count++] = (byte)Clamp(0, 255, (int)(y * 1.164 + u * 2.018));
                dst[count++] = (byte)Clamp(0, 255, (int)(y * 1.164 - v * 0.813 - u * 0.391));
                dst[count++] = (byte)Clamp(0, 255, (int)(y * 1.164 + v * 1.596));

                Y = src[i+2];
                y = Y - 16;
                dst[count++] = (byte)Clamp(0, 255, (int)(y * 1.164 + u * 2.018));
                dst[count++] = (byte)Clamp(0, 255, (int)(y * 1.164 - v * 0.813 - u * 0.391));
                dst[count++] = (byte)Clamp(0, 255, (int)(y * 1.164 + v * 1.596));
            }
        }

        //Clamp a number in a range set.
        private int Clamp(int min, int max, int val)
        {
            if (val < min) return min;
            else if (val > max) return max;
            return val;
        }

 

    }
}
