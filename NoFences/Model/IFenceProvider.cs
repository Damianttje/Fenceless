using System;

namespace Fenceless.Model
{
    public interface IFenceProvider : IDisposable
    {
        event Action ItemsChanged;
        void Refresh();
    }
}
