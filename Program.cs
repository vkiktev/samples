using Structurizr;
using Structurizr.Api;
using Structurizr.Documentation;

namespace structurizr
{
    /// <summary>
    /// This is a simple example of how to get started with Structurizr for .NET.
    /// </summary>
    class Program
    {
        private const long WorkspaceId = 37816;
        private const string ApiKey = "ee534180-cd0e-4d23-876e-ddc1eac878b9";
        private const string ApiSecret = "8e19947b-2aae-4a76-9a31-511e72b29d9e";

        private const string MicroserviceTag = "Microservice";
        private const string MessageBusTag = "Message Bus";
        private const string DataStoreTag = "Database";

        static void Main(string[] args)
        {
            // a Structurizr workspace is the wrapper for a software architecture model, views and documentation
            Workspace context = CreateContextDiagram();

            UploadWorkspaceToStructurizr(context);
        }

        private static Workspace CreateContextDiagram()
        {
            var workspace = new Workspace("Context", "This is a model of InPlacer content storage.");
            Model model = workspace.Model;

            // add some elements to your software architecture model
            Person person = model.AddPerson("Unauthorized user", "A user before an authorization.");
            Person user = model.AddPerson("Authorized user", "A user after an authorization.");
            SoftwareSystem softwareSystem = model.AddSoftwareSystem(Location.Internal, "InPlacer System", "System for storing content");
            person.Uses(softwareSystem, "Uses");
            user.Uses(softwareSystem, "Uses");

            Person customer = model.AddPerson("Customer", "A user of the system.");

            SoftwareSystem oauth0 = model.AddSoftwareSystem(Location.External, "OAuth0.com", "SAAS for authentication using OAuth protocol.");
            softwareSystem.Uses(oauth0, "Uses");
            SoftwareSystem webSites = model.AddSoftwareSystem(Location.External, "External Web", "A huge number of Web-sites which will be grubed when new link URL get from customer.");
            softwareSystem.Uses(webSites, "Uses");

            Container webApp = softwareSystem.AddContainer("Web Application", "Allows users to view stored content and manage their own content (add, change, delete, attach tags etc.).", "ASP.NET Core 2.x, Nginx, React JS/Redux, Bootstrap, NLog");
            person.Uses(webApp, "Uses [HTTPS]");
            user.Uses(webApp, "Uses [HTTPS]");
            customer.Uses(webApp, "Uses [HTTPS]");

            Container chromeExt = softwareSystem.AddContainer("Chrome Extension", "Allows users to add link URL to the content to the InPlacer System.", "JavaScript");
            person.Uses(chromeExt, "Uses [HTTPS]");
            user.Uses(chromeExt, "Uses [HTTPS]");
            customer.Uses(chromeExt, "Uses [HTTPS]");

            Container mobileApp = softwareSystem.AddContainer("Android Application", "Allows users to view stored content and manage their own content (add, change, delete, attach tags etc.).", "Java, Android SDK");
            person.Uses(mobileApp, "Uses [HTTPS]");
            user.Uses(mobileApp, "Uses [HTTPS]");
            customer.Uses(mobileApp, "Uses [HTTPS]");

            Container webApi = softwareSystem.AddContainer("Web API", "The entry point for all interactions with backend part of the system.", "ASP.NET Core 2.x, Nginx, MongoDB.Driver, Elastic.Driver, RabbitMQ.Driver, InflaxData.Driver etc.");
            webApi.AddTags(MicroserviceTag);
            webApp.Uses(webApi, "Uses [HTTP]");
            chromeExt.Uses(webApi, "Uses [HTTPS]");
            mobileApp.Uses(webApi, "Uses [HTTPS]");
            webApi.Uses(oauth0, "Uses [HTTPS]");

            // Work with users
            var accountController = webApi.AddComponent("AccountController", "Authentication and Authorization functionality");
            var linkController = webApi.AddComponent("LinkController", "Works with links which customers put from any devices");
            var contentController = webApi.AddComponent("ContentController", "Manages contents");

            customer.Uses(accountController, "Uses");
            customer.Uses(linkController, "Uses");
            customer.Uses(contentController, "Uses");

            var databaseContext = webApi.AddComponent("DatabaseContext", "Uses for working with database");

            accountController.Uses(oauth0, "Uses");
            accountController.Uses(databaseContext, "Uses");
            linkController.Uses(databaseContext, "Uses");
            contentController.Uses(databaseContext, "Uses");

            Container mongoDb = softwareSystem.AddContainer("NoSQL Data Store", "Stores content and metadata of the content (URL, keywords, tags etc.).", "MongoDB 3.6.x");
            mongoDb.AddTags(DataStoreTag);
            webApi.Uses(mongoDb, "Uses [HTTP]");

            databaseContext.Uses(mongoDb, "Reads from and Writes to");

            Container elastic = softwareSystem.AddContainer("Indexing system", "Indexes incoming content for future analysis opportunity.", "Elastic Search 5.x");
            elastic.AddTags(DataStoreTag);
            webApi.Uses(elastic, "Uses [HTTP]");

            Container messageBus = softwareSystem.AddContainer("Message Bus", "Transport for business events", "RabbitMQ 3.7.x");
            messageBus.AddTags(MessageBusTag);
            webApi.Uses(messageBus, "Uses [HTTP]", "", InteractionStyle.Asynchronous);

            Container backgroundService = softwareSystem.AddContainer("Operational service", "Works background, interracts with queue and indexing systems for make some delayed work.", "dotnet core 2.x");
            backgroundService.AddTags(MicroserviceTag);
            backgroundService.Uses(mongoDb, "Uses [HTTP]");
            messageBus.Uses(backgroundService, "Uses [HTTP]");
            backgroundService.Uses(webSites, "Uses [HTTPS]");

            Container monitoring = softwareSystem.AddContainer("Monitoring", "Adds support of the monitoring.", "Kibana");
            monitoring.AddTags(MicroserviceTag);
            webApi.Uses(monitoring, "Uses [HTTP]");
            monitoring.Uses(elastic, "Uses [HTTP]");

            // define some views (the diagrams you would like to see)
            ViewSet views = workspace.Views;
            SystemContextView contextView = views.CreateSystemContextView(softwareSystem, "SystemContext",
                "System Context diagram for InPlacer content storage.");
            contextView.PaperSize = PaperSize.A4_Landscape;
            contextView.AddAllSoftwareSystems();
            contextView.Add(user);
            contextView.Add(person);

            var containerView = views.CreateContainerView(softwareSystem, "SystemContainer",
                "System Container diagram for InPlacer content storage");
            contextView.PaperSize = PaperSize.A4_Landscape;
            containerView.Add(customer);
            containerView.AddAllContainers();
            containerView.Add(oauth0);
            containerView.Add(webSites);

            var webAppComponentView = views.CreateComponentView(webApi, "WebApiComponent",
                "Web API Container diagram for InPlacer content storage");
            webAppComponentView.PaperSize = PaperSize.A4_Landscape;
            webAppComponentView.Add(customer);
            webAppComponentView.Add(accountController);
            webAppComponentView.Add(linkController);
            webAppComponentView.Add(contentController);
            webAppComponentView.Add(databaseContext);
            webAppComponentView.Add(oauth0);
            webAppComponentView.Add(mongoDb);

            // add some documentation
            StructurizrDocumentationTemplate template = new StructurizrDocumentationTemplate(workspace);
            template.AddContextSection(softwareSystem, Format.Markdown,
                "Here is some context about the software system...\n" +
                "\n" +
                "![](embed:SystemContext)");

            // add some styling
            Styles styles = views.Configuration.Styles;
            styles.Add(new RelationshipStyle(Tags.Relationship) { Routing = Routing.Direct });
            styles.Add(new ElementStyle(Tags.SoftwareSystem) { Background = "#1168bd", Color = "#ffffff", Shape = Shape.RoundedBox });
            styles.Add(new ElementStyle(Tags.Container) { Background = "#facc2E" });
            styles.Add(new ElementStyle(Tags.Person) { Background = "#08427b", Color = "#ffffff", Shape = Shape.Person });
            styles.Add(new ElementStyle(MessageBusTag) { Shape = Shape.Pipe, Width = 1200 });
            styles.Add(new ElementStyle(MicroserviceTag) { Shape = Shape.Hexagon });
            styles.Add(new ElementStyle(DataStoreTag) { Background = "#f5da81", Shape = Shape.Cylinder });

            styles.Add(new ElementStyle(Tags.Component) { Background = "#D4F3C0", Color = "#000000", Shape = Shape.RoundedBox });


            return workspace;
        }

        private static void UploadWorkspaceToStructurizr(Workspace workspace)
        {
            StructurizrClient structurizrClient = new StructurizrClient(ApiKey, ApiSecret);
            structurizrClient.PutWorkspace(WorkspaceId, workspace);
        }
    }
}

