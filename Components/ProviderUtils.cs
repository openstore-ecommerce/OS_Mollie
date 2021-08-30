using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using Mollie.Api.Client;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models.Payment.Response;
using NBrightCore.common;
using NBrightDNN;
using Nevoweb.DNN.NBrightBuy.Components;

namespace OS_Mollie
{
    public class ProviderUtils
    {
        #region GetProviderSettings
        public static NBrightInfo GetProviderSettings()
        {
            var objCtrl = new NBrightBuyController();
            var info = objCtrl.GetPluginSinglePageData("OS_Molliepayment", "OS_MolliePAYMENT", Utils.GetCurrentCulture());
            return info;
        }
        #endregion

        public static PaymentResponse GetOrderPaymentResponse(OrderData orderData, string origin)
        {
            var info = ProviderUtils.GetProviderSettings();
            var apiKey = info.GetXmlProperty("genxml/textbox/key");

            IPaymentClient paymentClient = new PaymentClient(apiKey);
            var task = Task.Run(async () => await paymentClient.GetPaymentAsync(orderData.PaymentPassKey));
            task.Wait();
            PaymentResponse paymentClientResult = task.Result;

            if (string.IsNullOrEmpty(orderData.GetInfo().GetXmlNode("genxml/paymentreturn")))
            {
                orderData.GetInfo().AddSingleNode("paymentreturn", "", "genxml");
            }

            var xmlDoc = orderData.GetInfo().XMLDoc;
            var newNode = xmlDoc.CreateNode(XmlNodeType.Element, "data", "");
            var dtAttr = newNode.Attributes.Append(xmlDoc.CreateAttribute("timestamp"));
            dtAttr.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            newNode.Attributes.Append(dtAttr);

            var xml = $@"<data timestamp=""{DateTime.Now:yyyy-MM-dd HH:mm:ss}"" origin=""{origin}"">
    <created>{paymentClientResult.CreatedAt:yyyy-MM-dd HH:mm:ss}</created>
    <cancelled>{paymentClientResult.CreatedAt:yyyy-MM-dd HH:mm:ss}</cancelled>
    <expired>{paymentClientResult.CreatedAt:yyyy-MM-dd HH:mm:ss}</expired>
    <failed>{paymentClientResult.CreatedAt:yyyy-MM-dd HH:mm:ss}</failed>
    <paied>{paymentClientResult.CreatedAt:yyyy-MM-dd HH:mm:ss}</paied>
    <description>{paymentClientResult.Description}</description>
    <method>{paymentClientResult.Method}</method>
    <amount>{paymentClientResult.Amount.Value}</amount>
    <currency>{paymentClientResult.Amount.Currency}</currency>
    <status>{paymentClientResult.Status}</status>
</data>";

                orderData.GetInfo().AddXmlNode(xml, "data", "genxml/paymentreturn");
                var modCtrl = new NBrightBuyController();
                modCtrl.Update(orderData.GetInfo());

                return paymentClientResult;
        }
    }
}