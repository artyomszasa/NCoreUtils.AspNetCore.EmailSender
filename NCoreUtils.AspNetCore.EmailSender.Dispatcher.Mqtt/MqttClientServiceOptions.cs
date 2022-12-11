namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher;

public partial record MqttClientServiceOptions(string Topic) : IMqttClientServiceOptions;