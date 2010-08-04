﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.4005
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace QutSensors.AnalysisProcessor.ProcessorService {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="sensor.mquter.qut.edu.au", ConfigurationName="ProcessorService.ProcessorService")]
    public interface ProcessorService {
        
        [System.ServiceModel.OperationContractAttribute(Action="sensor.mquter.qut.edu.au/ProcessorService/GetWorkItem", ReplyAction="sensor.mquter.qut.edu.au/ProcessorService/GetWorkItemResponse")]
        QutSensors.Shared.AnalysisWorkItem GetWorkItem(string workerName);
        
        [System.ServiceModel.OperationContractAttribute(Action="sensor.mquter.qut.edu.au/ProcessorService/GetWorkItems", ReplyAction="sensor.mquter.qut.edu.au/ProcessorService/GetWorkItemsResponse")]
        QutSensors.Shared.AnalysisWorkItem[] GetWorkItems(string workerName, int maxItems);
        
        [System.ServiceModel.OperationContractAttribute(Action="sensor.mquter.qut.edu.au/ProcessorService/ReturnWorkItemIncomplete", ReplyAction="sensor.mquter.qut.edu.au/ProcessorService/ReturnWorkItemIncompleteResponse")]
        void ReturnWorkItemIncomplete(string workerName, int jobItemId, string itemRunDetails, bool errorOccurred);
        
        [System.ServiceModel.OperationContractAttribute(Action="sensor.mquter.qut.edu.au/ProcessorService/ReturnWorkItemComplete", ReplyAction="sensor.mquter.qut.edu.au/ProcessorService/ReturnWorkItemCompleteResponse")]
        void ReturnWorkItemComplete(string workerName, int jobItemId, string itemRunDetails, QutSensors.Shared.ProcessorResultTag[] results);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    public interface ProcessorServiceChannel : QutSensors.AnalysisProcessor.ProcessorService.ProcessorService, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
    public partial class ProcessorServiceClient : System.ServiceModel.ClientBase<QutSensors.AnalysisProcessor.ProcessorService.ProcessorService>, QutSensors.AnalysisProcessor.ProcessorService.ProcessorService {
        
        public ProcessorServiceClient() {
        }
        
        public ProcessorServiceClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public ProcessorServiceClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public ProcessorServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public ProcessorServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public QutSensors.Shared.AnalysisWorkItem GetWorkItem(string workerName) {
            return base.Channel.GetWorkItem(workerName);
        }
        
        public QutSensors.Shared.AnalysisWorkItem[] GetWorkItems(string workerName, int maxItems) {
            return base.Channel.GetWorkItems(workerName, maxItems);
        }
        
        public void ReturnWorkItemIncomplete(string workerName, int jobItemId, string itemRunDetails, bool errorOccurred) {
            base.Channel.ReturnWorkItemIncomplete(workerName, jobItemId, itemRunDetails, errorOccurred);
        }
        
        public void ReturnWorkItemComplete(string workerName, int jobItemId, string itemRunDetails, QutSensors.Shared.ProcessorResultTag[] results) {
            base.Channel.ReturnWorkItemComplete(workerName, jobItemId, itemRunDetails, results);
        }
    }
}
