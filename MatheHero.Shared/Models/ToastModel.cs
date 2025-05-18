using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatheHero.Shared.Models
{
    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public enum MessageType
    {
        DEFAULT,
        EMAIL_EXISTS,
        INVALID_EMAIL,
        WEAK_PASSWORD,
        EMAIL_NOT_FOUND,
        INVALID_PASSWORD,
        USER_DISABLED,
        TOO_MANY_ATTEMPTS_TRY_LATER,
        OPERATION_NOT_ALLOWED,
        USER_INPUT_MISSING,
        UNKNOWN_ERROR,
        RESPONSE_ERROR,
        INVALID_LOGIN_CREDENTIALS,
    }

    public class ToastMessage
    {
        public ToastType Type { get; set; }

        public MessageType EnumMessage { get; set; }

        public string Message { get; set; }

        public int Duration { get; set; } = 4000;
    }
}
