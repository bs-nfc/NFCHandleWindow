using System.Windows.Forms;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using NFCHandleWindow.Utils;

namespace NFCHandleWindow.Forms
{
    public partial class NFCHandleForm : Form
    {
        [DllImport("User32.dll")]
        extern static UInt32 RegisterWindowMessage(string lpString);

        private const UInt32 DEVICE_TYPE_NFC_18092_212K = 0x00000002;

        private const UInt32 DEVICE_TYPE_NFC_18092_424K = 0x00000004;

        private const string MsgStrOfFind = "find";

        private const string MsgStrOfEnable = "enable";

        private static felica_nfc_dll_wrapper FeliCaNfcDllWrapperClass = new felica_nfc_dll_wrapper();

        private UInt32 cardFindMessage;

        private UInt32 cardEnableMessage;

        public NFCHandleForm()
        {
            InitializeComponent();
        }

        private void HandleForm_Shown(object sender, EventArgs e)
        {
            bool result;

            // NFCライブラリを初期化します
            result = FeliCaNfcDllWrapperClass.FeliCaLibNfcInitialize();
            if (!result)
            {
                Console.Error.WriteLine("Failed to nfc initialize");
                Close();
                return;
            }

            // NFCライブラリを開きます
            StringBuilder portName = new StringBuilder("USB0");
            result = FeliCaNfcDllWrapperClass.FeliCaLibNfcOpen(portName);
            if (!result)
            {
                Console.Error.WriteLine("Failed to nfc open, port_name=" + portName);
                HandleError();
                Close();
                return;
            }

            // NFCデバイス補足時のWindowメッセージを登録します。
            cardFindMessage = RegisterWindowMessage(MsgStrOfFind);
            if (cardFindMessage == 0)
            {
                Console.Error.WriteLine("Failed to register window message, param=" + MsgStrOfFind);
                Close();
                return;
            }

            cardEnableMessage = RegisterWindowMessage(MsgStrOfEnable);
            if (cardEnableMessage == 0)
            {
                Console.Error.WriteLine("Failed to register window message, param=" + MsgStrOfEnable);
                Close();
                return;
            }

            result = FeliCaNfcDllWrapperClass.FeliCaLibNfcSetPollCallbackParameters(this.Handle, MsgStrOfFind, MsgStrOfEnable);
            if (!result)
            {
                Console.Error.WriteLine("Failed to set pall callback parameters, msf_str_of_find=" + MsgStrOfFind + ", msg_str_of_enable=" + MsgStrOfEnable);
                HandleError();
                Close();
                return;
            }

            // 補足するデバイスの種類を決定します。
            UInt32 targetDevice = 0;
            targetDevice = targetDevice | DEVICE_TYPE_NFC_18092_212K;
            targetDevice = targetDevice | DEVICE_TYPE_NFC_18092_424K;

            // ポーリングを開始します。
            FeliCaNfcDllWrapperClass.FeliCaLibNfcStartPollMode(targetDevice);
            if (!result)
            {
                Console.Error.WriteLine("Failed to start poll mode, target_device=" + targetDevice);
                HandleError();
                Close();
                return;
            }
        }

        /// <summary>
        /// エラー番号をエラー出力ストリームに出力した後
        /// NFCライブラリの終了処理をします。
        /// </summary>
        private void HandleError()
        {
            // エラー番号を出力します。
            UInt32[] errorInfo = new UInt32[2] { 0, 0 };
            FeliCaNfcDllWrapperClass.FeliCaLibNfcGetLastError(errorInfo);
            Console.Error.WriteLine("Last error");
            Console.Error.WriteLine(errorInfo[0]);
            Console.Error.WriteLine(errorInfo[1]);

            // NFCライブラリの終了処理をします
            FeliCaNfcDllWrapperClass.FeliCaLibNfcStopPollMode();
            FeliCaNfcDllWrapperClass.FeliCaLibNfcClose();
            FeliCaNfcDllWrapperClass.FeliCaLibNfcUninitialize();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == cardFindMessage)
            {
                OnReceiveFoundCardMessage(ref m);
            }
            else if (m.Msg == cardEnableMessage)
            {
                OnReceiveCardEnabledMessage(ref m);
            }

            base.WndProc(ref m);
            return;
        }

        protected virtual void OnReceiveFoundCardMessage(ref Message m)
        {
            IntPtr pDevInfo = m.LParam;
            IntPtr pDeviceData;
            if (IntPtr.Size == 8)
            {
                pDeviceData = (IntPtr)((Int64)pDevInfo
                    + (Int64)Marshal.OffsetOf(typeof(DEVICE_INFO), "dev_info"));
            }
            else
            {
                pDeviceData = (IntPtr)((Int32)pDevInfo
                    + (Int32)Marshal.OffsetOf(typeof(DEVICE_INFO), "dev_info"));
            }

            DEVICE_INFO dev_info = (DEVICE_INFO)Marshal.PtrToStructure(pDevInfo, typeof(DEVICE_INFO));
            switch (dev_info.target_device)
            {
                case DEVICE_TYPE_NFC_18092_212K:
                case DEVICE_TYPE_NFC_18092_424K:
                        DEVICE_DATA_NFC_18092_212_424K deviceData_F =
                            (DEVICE_DATA_NFC_18092_212_424K)Marshal.PtrToStructure(pDeviceData,
                            typeof(DEVICE_DATA_NFC_18092_212_424K));

                        string idm = Util.ToHexString(deviceData_F.NFCID2);
                        MessageBox.Show(this, idm, "discover nfc tag");

                        break;
            }

            FeliCaNfcDllWrapperClass.FeliCaLibNfcStopDevAccess(0x00);
        }

        protected virtual void OnReceiveCardEnabledMessage(ref Message m)
        {
        }

        private void HandleForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // NFCライブラリの終了処理をします
            FeliCaNfcDllWrapperClass.FeliCaLibNfcStopPollMode();
            FeliCaNfcDllWrapperClass.FeliCaLibNfcClose();
            FeliCaNfcDllWrapperClass.FeliCaLibNfcUninitialize();
        }
    }
}
