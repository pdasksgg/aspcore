using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Algorithm
{
    public class RequestDo
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Passoword { get; set; }
        public string Name { get; set; }

    }

    public class IDVPersonalData
    {
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string ZipCode { get; set; }
    }

    public class IDVRequest : RequestDo
    {
        public IDVPersonalData PersonData { get; set; }
    }

    public class OfacDecisionData : ResponseO
    {
        public IDVPersonalData PersonData { get; set; }
    }

    public class IDVDecisionData : ResponseO
    {
        public IDVPersonalData PersonData { get; set; }
    }

    public class ResponseO
    {
        public string Credential { get; set; }
    }

    public class CommonServiceAgent
    {
        public ResponseO Execute(RequestDo request)
        {
            return new OfacDecisionData { Credential = request.Name + "" + request.Id };
        }
    }

    public interface IInfrastructure<out T>
    {
        T Instance { get; }
    }

    public interface IServiceAgent<T> where T : class
    {
        T GetServiceAgent();
    }

    public class ServiceAgentBuilder<T> : IServiceAgent<T> where T : class, new()
    {
        public T GetServiceAgent()
        {
            return new T();
        }
    }

    public interface IExecute<out TResposne, in TRequest> where TResposne : class where TRequest : class
    {
        TResposne Execute(TRequest request);
    }

    public class ExceuteSA<TResposne, TRequest> : IExecute<TResposne, TRequest>
                                                  where TResposne : ResponseO
                                                  where TRequest : RequestDo
    {
        IServiceAgent<CommonServiceAgent> _serviceAgent;

        public ExceuteSA(IServiceAgent<CommonServiceAgent> serviceAgent)
        {
            _serviceAgent = serviceAgent;
        }

        public TResposne Execute(TRequest request)
        {
            var getType = typeof(TResposne);
            var response = _serviceAgent.GetServiceAgent().Execute(request);
            if (response is TResposne)
            {

            }
            return null;

        }
    }

}
