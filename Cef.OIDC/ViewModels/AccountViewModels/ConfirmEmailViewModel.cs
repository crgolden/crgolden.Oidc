﻿namespace Cef.OIDC.ViewModels.AccountViewModels
{
    using System.ComponentModel.DataAnnotations;

    public class ConfirmEmailViewModel
    {
        [Required]
        public string Code { get; set; }

        [Required]
        public string UserId { get; set; }
    }
}