namespace Snooper
{
    internal class PluginState
    {
        public bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }
    }
}
