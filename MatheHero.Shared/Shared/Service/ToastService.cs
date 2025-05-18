using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatheHero.Shared.Models;

namespace MatheHero.Shared.Shared.Service
{
    public class ToastService
    {
        public event Action<ToastMessage> OnShow;

        public void ShowToast(string message, MessageType messageType, ToastType type = ToastType.Info, int duration = 4000)
        {
            OnShow?.Invoke(new ToastMessage
            {
                Message = message,
                EnumMessage = messageType,
                Type = type,
                Duration = duration
            });                
        }
    }
}
