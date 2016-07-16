using System;

namespace Utils
{
    public class DisposeObject : IDisposable
    {
        //
        // Fields
        //
        protected bool m_IsDispose;

        //
        // Constructors
        //
        ~DisposeObject()
        {
            Dispose(false);
        }

        //
        // Methods
        //
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool Diposing)
        {
            if (m_IsDispose)
            {
                return;
            }
            OnFree(Diposing);
            m_IsDispose = true;
        }

        protected virtual void OnFree(bool isManual)
        {
        }
    }
}
