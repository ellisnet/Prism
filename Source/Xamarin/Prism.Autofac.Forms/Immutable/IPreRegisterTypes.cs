using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Prism.Autofac.Forms
{
    public interface IPreRegisterTypes
    {
        void RegisterTypes(IAutofacContainer container);
    }
}
