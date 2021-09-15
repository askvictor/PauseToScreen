using System;

namespace PauseToScreen
{
    // Detects when a DISPLAYCHANGE event occurs, and runs the supplied action
    public class DisplayHotplugDetect : System.Windows.Forms.NativeWindow
    {
        private Action theAction;

        public DisplayHotplugDetect(Action aAction)
        {
            this.theAction = aAction;
            CreateHandle(new System.Windows.Forms.CreateParams());
        }

        private static int WM_DISPLAYCHANGE = 0x007e;

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_DISPLAYCHANGE)
            {
                theAction();
            }
        }
    }
}