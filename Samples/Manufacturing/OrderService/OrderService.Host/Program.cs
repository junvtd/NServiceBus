using System;
using Common.Logging;
using NHibernate.Cfg;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Saga;
using OrderService.Persistence;
using NHibernate;
using ObjectBuilder;

namespace OrderService.Host
{
    class Program
    {
        static void Main()
        {
            LogManager.GetLogger("hello").Debug("Order Started.");
            ObjectBuilder.SpringFramework.Builder builder = new ObjectBuilder.SpringFramework.Builder();

            try
            {
                Configuration config = new Configuration();
                config.Configure();

                ISessionFactory sessionFactory = config.BuildSessionFactory();

                NServiceBus.Config.Configure.With(builder)
                    .BinarySerializer()
                    .MsmqTransport()
                        .IsTransactional(true)
                        .PurgeOnStartup(false)
                    .MsmqSubscriptionStorage()
                    .NHibernateSagaPersister(sessionFactory)
                    .UnicastBus()
                        .ImpersonateSender(false)
                        .SetMessageHandlersFromAssembliesInOrder(
                            typeof(GridInterceptingMessageHandler).Assembly
                            , typeof(SagaMessageHandler).Assembly
                            , typeof(OrderSagaFinder).Assembly
                            , typeof(OrderSaga).Assembly
                        );

                builder.ConfigureComponent<OrderSagaFinder>(ComponentCallModelEnum.Singlecall)
                    .SessionFactory = sessionFactory;

                IBus bServer = builder.Build<IBus>();
                bServer.Start();
            }
            catch (Exception e)
            {
                LogManager.GetLogger("hello").Fatal("Exiting", e);
                Console.Read();
            }

            Console.Read();

        }
    }
}
