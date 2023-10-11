using System;
using FluentValidation;
using Template.Payments.Manual.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;
using Nop.Web.Framework.Models;

namespace Template.Payments.Manual.Validators;

public partial class PaymentInfoValidator<T> : BaseNopValidator<T> where T : BaseNopModel, ITemplatePaymentInfoModel
{
    private readonly TemplateDescriptorUtility TemplateDescriptorUtility;

    public PaymentInfoValidator(ILocalizationService localizationService, TemplateDescriptorUtility templateDescriptorUtility)
    {
        // useful links:
        // http://fluentvalidation.codeplex.com/wikipage?title=Custom&referringTitle=Documentation&ANCHOR#CustomValidator
        // http://benjii.me/2010/11/credit-card-validator-attribute-for-asp-net-mvc-3/
        TemplateDescriptorUtility = templateDescriptorUtility;

        RuleFor(x => x.OperationNumber).NotEmpty().WithMessageAwait(localizationService.GetResourceAsync($"Plugins.{TemplateDescriptorUtility.SystemName}.Fields.OperationNumber.Required"));
        RuleFor(x => x.OperationNumber).Length(5, 5).WithMessageAwait(localizationService.GetResourceAsync($"Plugins.{TemplateDescriptorUtility.SystemName}.Fields.OperationNumber.WrongLength"));
        RuleFor(x => x.OperationNumber).Matches(@"^[0-9]+$").WithMessageAwait(localizationService.GetResourceAsync($"Plugins.{TemplateDescriptorUtility.SystemName}.Fields.OperationNumber.Wrong"));
        
    }
}