using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Cassette.Utilities;

namespace Cassette.UI
{
    public class ReferenceBuilder<T> : IReferenceBuilder<T>
        where T: Module
    {
        public ReferenceBuilder(IModuleContainer<T> moduleContainer, IModuleFactory<T> moduleFactory, IPlaceholderTracker placeholderTracker, ICassetteApplication application)
        {
            this.moduleContainer = moduleContainer;
            this.moduleFactory = moduleFactory;
            this.placeholderTracker = placeholderTracker;
            this.application = application;
        }

        readonly IModuleContainer<T> moduleContainer;
        readonly IModuleFactory<T> moduleFactory;
        readonly IPlaceholderTracker placeholderTracker;
        readonly ICassetteApplication application;
        readonly Dictionary<string, List<Module>> modulesByLocation = new Dictionary<string, List<Module>>();
        readonly HashSet<string> renderedLocations = new HashSet<string>();
 
        public void AddReference(string path, string location = null)
        {
            path = PathUtilities.AppRelative(path);

            var module = moduleContainer.FindModuleContainingPath(path);
            if (module == null && path.IsUrl())
            {
                // Ad-hoc external module reference.
                module = moduleFactory.CreateExternalModule(path);
            }

            if (module == null)
            {
                throw new ArgumentException("Cannot find an asset module containing the path \"" + path + "\".");                
            }

            // Module can define it's own prefered location. Use this when we aren't given
            // an explicit location argument i.e. null.
            if (location == null)
            {
                location = module.Location;
            }

            AddReference(module, location);
        }

        public void AddReference(Module module, string location)
        {
            var modules = GetOrCreateModuleSet(location);
            if (modules.Contains(module)) return;
            modules.Add(module);
        }

        public IEnumerable<Module> GetModules(string location)
        {
            var modules = GetOrCreateModuleSet(location);
            return moduleContainer.IncludeReferencesAndSortModules(modules);
        }

        public IHtmlString Render(string location = null)
        {
            renderedLocations.Add(location ?? "");
            return placeholderTracker.InsertPlaceholder(
                () => CreateHtml(location)
            );
        }

        public string ModuleUrl(string path)
        {
            var module = moduleContainer.FindModuleContainingPath(path);
            if (module == null)
            {
                throw new ArgumentException("Cannot find module contain path \"" + path + "\".");
            }
            return application.UrlGenerator.CreateModuleUrl(module);
        }

        HtmlString CreateHtml(string location)
        {
            return new HtmlString(string.Join(Environment.NewLine,
                GetModules(location).Select(
                    module => module.Render(application).ToHtmlString()
                )
            ));
        }

        List<Module> GetOrCreateModuleSet(string location)
        {
            location = location ?? ""; // Dictionary doesn't accept null keys.
            List<Module> modules;
            if (modulesByLocation.TryGetValue(location, out modules))
            {
                return modules;
            }
            else
            {
                modules = new List<Module>();
                modulesByLocation.Add(location, modules);
                return modules;
            }
        }
    }
}