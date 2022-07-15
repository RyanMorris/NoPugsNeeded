using System;
using System.Windows.Input;

namespace NoPugsNeeded.Events
{
    public class ControllerEvent
    {
        public event EventHandler<Key> SpecialKeyPressed;

        public virtual void OnSpecialKeyPressed(Key key)
        {
            SpecialKeyPressed?.Invoke(this, key);
        }
    }
}
