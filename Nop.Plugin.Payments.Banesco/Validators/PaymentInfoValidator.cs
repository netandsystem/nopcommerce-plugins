using System;
using FluentValidation;
using Nop.Plugin.Payments.Banesco.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Payments.Banesco.Validators
{
    public partial class PaymentInfoValidator : BaseNopValidator<PaymentInfoModel>
    {
        public PaymentInfoValidator(ILocalizationService localizationService)
        {
            // useful links:
            // http://fluentvalidation.codeplex.com/wikipage?title=Custom&referringTitle=Documentation&ANCHOR#CustomValidator
            // http://benjii.me/2010/11/credit-card-validator-attribute-for-asp-net-mvc-3/

            RuleFor(x => x.OperationNumber).NotEmpty(); //.WithMessageAwait(localizationService.GetResourceAsync("Plugins.Payments.Banesco.Fields.OperationNumber.Required"));
            RuleFor(x => x.OperationNumber).Length(0, 5); //.WithMessageAwait(localizationService.GetResourceAsync("Plugins.Payments.Banesco.Fields.OperationNumber.WrongLength"));
            RuleFor(x => x.OperationNumber).Matches(@"^[0-9]{3,4}$"); //.WithMessageAwait(localizationService.GetResourceAsync("Plugins.Payments.Banesco.Fields.OperationNumber.Wrong"));
        }
    }
}