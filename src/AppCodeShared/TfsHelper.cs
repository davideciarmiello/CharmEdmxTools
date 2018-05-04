using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.Win32;

namespace AppCodeShared
{
    public class TfsHelper
    {
        private string _projectFullName;
        public TfsHelper(string projectFullName)
        {
            _projectFullName = projectFullName;
        }

        public TfsTeamProjectCollection connection;
        public VersionControlServer vcs;
        public Workspace workspace;

        private bool? _connected;
        public bool Connect(bool throwEx = false)
        {
            if (_connected == true)
                return _connected.Value;
            try
            {
                WorkingFolder tfsFolder = null;
                var workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(_projectFullName);
                if (workspaceInfo == null)
                {
                    ConnectMethodTwo(ref tfsFolder);
                    if (connection == null)
                        throw new Exception("Connect.workspaceInfo is null - ProjectFullName: " + _projectFullName + ".\r\nTry to launch tfs cache clear with: \r\ntf workspaces /collection:http://tfs.domain.com/DefaultCollection");
                    EnsureUpdateWorkspaceInfoCache();
                    //MessageBox.Show(string.Format("GetLocalWorkspaceInfo recuperato dopo {0} tentativi", i + 1), "Errore connessione TFS", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                else
                {
                    var serverUri = workspaceInfo.ServerUri;
                    if (serverUri == null)
                        throw new Exception("Connect.serverUri is null");
                    connection = new TfsTeamProjectCollection(serverUri);
                }

                if (connection == null)
                    throw new Exception("Connect.connection is null");
                vcs = vcs ?? connection.GetService<VersionControlServer>();
                if (vcs == null)
                    throw new Exception("Connect.vcs is null");
                workspace = workspace ?? vcs.GetWorkspace(_projectFullName);
                if (workspace == null)
                    throw new Exception("Connect.workspace is null");
                //MessageBox.Show("TFS Connected", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                tfsFolder = tfsFolder ?? workspace.Folders
                    .FirstOrDefault(x => _projectFullName == x.LocalItem || _projectFullName.StartsWith(string.Concat(x.LocalItem, "\\"), StringComparison.OrdinalIgnoreCase));

                _workspaceFolderTfsRoot = tfsFolder == null ? "$/" : tfsFolder.ServerItem;
                _connected = true;
            }
            catch (Exception ex)
            {
                if (throwEx)
                {
                    _connected = false;
                    throw;
                }
                MessageBox.Show(ex.ToString(), "Errore connessione TFS", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _connected = false;
            }
            return _connected.Value;
        }

        private void ConnectMethodTwo(ref WorkingFolder tfsFolder)
        {
            var tfsServers = GetTfsServersForDir(new FileInfo(_projectFullName).Directory);
            Exception lastEx = null;
            foreach (var tfsServer in tfsServers)
            {
                TfsTeamProjectCollection tfsCon = null;
                try
                {
                    tfsCon = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(tfsServer));
                    tfsCon.EnsureAuthenticated();
                    var versionControl = tfsCon.GetService<VersionControlServer>();

                    var username = versionControl.AuthorizedUser;
                    var workspaces = versionControl.QueryWorkspaces(null, username, Environment.MachineName);
                    foreach (Workspace w in workspaces)
                    {
                        foreach (WorkingFolder f in w.Folders)
                        {
                            if (_projectFullName == f.LocalItem || _projectFullName.StartsWith(string.Concat(f.LocalItem, "\\")))
                            {
                                connection = tfsCon;
                                vcs = versionControl;
                                workspace = w;
                                tfsFolder = f;
                                return;
                            }
                        }
                    }
                    tfsCon.Dispose();
                }
                catch (Exception ex)
                {
                    if (tfsCon != null)
                        tfsCon.Dispose();
                    lastEx = ex;
                }
            }
            if (lastEx != null)
                throw lastEx;
        }

        private static IEnumerable<string> GetTfsServersForDir(DirectoryInfo dir)
        {
            var tfsServersHashset = new HashSet<string>();
            while (dir != null)
            {
                foreach (var slnFile in dir.EnumerateFiles("*.sln", SearchOption.TopDirectoryOnly).OrderByDescending(x => x.LastWriteTimeUtc))
                {
                    var sln = File.ReadAllLines(slnFile.FullName);
                    var tfsServers = sln.Where(x => x.TrimStart().StartsWith("SccTeamFoundationServer = "))
                        .Select(x => x.Replace("SccTeamFoundationServer = ", "").Trim());
                    foreach (var tfsServer in tfsServers)
                    {
                        if (!tfsServersHashset.Add(tfsServer.ToLowerInvariant()))
                            continue;
                        yield return tfsServer;
                    }
                }
                if (dir.EnumerateDirectories("$tf").Any())
                    dir = null;
                else
                    dir = dir.Parent;
            }

            using (var registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\VisualStudio"))
            {
                if (registryKey != null)
                {
                    var versions = registryKey.GetSubKeyNames();
                    foreach (var version in versions)
                    {
                        using (var instancesKey = registryKey.OpenSubKey(version + @"\TeamFoundation\Instances"))
                        {
                            if (instancesKey == null)
                                continue;
                            foreach (var instanceName in instancesKey.GetSubKeyNames())
                            {
                                using (var collectionsKey = instancesKey.OpenSubKey(instanceName + @"\Collections"))
                                {
                                    if (collectionsKey == null)
                                        continue;
                                    foreach (var collectionName in collectionsKey.GetSubKeyNames())
                                    {
                                        using (var collectionKey = collectionsKey.OpenSubKey(collectionName))
                                        {
                                            if (collectionKey == null)
                                                continue;
                                            var uri = collectionKey.GetValue("Uri") as string;
                                            if (uri != null && tfsServersHashset.Add(uri.ToLowerInvariant()))
                                            {
                                                yield return uri;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void EnsureUpdateWorkspaceInfoCache()
        {
            if (vcs == null) return;
            try
            {
                Workstation.Current.EnsureUpdateWorkspaceInfoCache(vcs, vcs.AuthorizedUser);
            }
            catch { }

        }

        public void Close()
        {
            Commit();
            EnsureUpdateWorkspaceInfoCache();
            if (connection != null)
                connection.Dispose();
        }

        public string _workspaceFolderTfsRoot;
        private PendingChange[] _pendingChanges;

        public PendingChange[] GetPendingChangesCached()
        {
            if (_pendingChanges == null)
                _pendingChanges = GetPendingChanges();
            return _pendingChanges;
        }

        public PendingChange[] GetPendingChanges()
        {
            var res = workspace.GetPendingChanges(_workspaceFolderTfsRoot, RecursionType.Full);
            _pendingChanges = res;
            return res;
        }

        private enum Operation
        {
            PendAdd,
            PendDelete,
            UndoAdd,
            UndoDelete
        }
        private readonly ConcurrentDictionary<string, Operation> _pendingLocalChanges = new ConcurrentDictionary<string, Operation>();

        public void PendAdd(string fullPath)
        {
            _pendingLocalChanges.AddOrUpdate(fullPath, Operation.PendAdd, (s, operation) => Operation.PendAdd);
            //if (!Connect())
            //    return -1;
            //UndoDelete(fullPath);
            //return workspace.PendAdd(fullPath);
        }

        public void PendDelete(string fullPath)
        {
            _pendingLocalChanges.AddOrUpdate(fullPath, Operation.PendDelete, (s, operation) => Operation.PendDelete);
            //if (!Connect())
            //    return -1;
            //UndoAdd(fullPath);
            //return workspace.PendDelete(fullPath);
        }

        public void UndoAdd(string fullPath)
        {
            _pendingLocalChanges.AddOrUpdate(fullPath, Operation.UndoAdd, (s, operation) => Operation.UndoAdd);

            //var pendingChanges = GetPendingChanges();
            //var dd = pendingChanges.Where(it => it.LocalItem == fullPath && it.IsAdd).ToArray();
            //if (dd.Length == 0)
            //    return 0;
            //var res = workspace.Undo(dd);
            //return res;
        }
        private int WorkspaceUndoAdd(IEnumerable<string> fullPaths, PendingChange[] pendingChanges = null)
        {
            pendingChanges = pendingChanges ?? GetPendingChanges();
            var dd = pendingChanges.Where(it => fullPaths.Contains(it.LocalItem) && it.IsAdd).ToArray();
            if (dd.Length == 0)
                return 0;
            var res = workspace.Undo(dd);
            return res;
        }

        public void UndoDelete(string fullPath)
        {
            _pendingLocalChanges.AddOrUpdate(fullPath, Operation.UndoDelete, (s, operation) => Operation.UndoDelete);
        }

        private void WorkspaceUndoDelete(IEnumerable<string> fullPaths, PendingChange[] pendingChanges = null)
        {
            pendingChanges = pendingChanges ?? GetPendingChanges();
            var dd = pendingChanges.Where(it => fullPaths.Contains(it.LocalItem) && it.IsDelete).ToArray();
            if (dd.Length == 0)
                return;
            var items = dd.Select(it => it.LocalItem).ToList();
            items.ForEach(it =>
            {
                try { File.Copy(it, it + ".tmptfs", true); SetNotReadOnly(it); File.Delete(it); }
                catch { }
            });
            var res = workspace.Undo(dd);
            items.ForEach(it =>
            {
                try { File.Copy(it + ".tmptfs", it, true); SetNotReadOnly(it + ".tmptfs"); File.Delete(it + ".tmptfs"); }
                catch { }
            });
        }

        public int Commit()
        {
            if (_pendingLocalChanges.Count == 0)
                return 0;
            if (!Connect())
                return -1;

            _pendingChanges = null;
            var itemsToUndoDelete = _pendingLocalChanges.Where(it => it.Value == Operation.PendAdd || it.Value == Operation.UndoDelete).Select(it => it.Key).ToList();
            if (itemsToUndoDelete.Any())
                WorkspaceUndoDelete(itemsToUndoDelete, GetPendingChangesCached());
            var itemsToUndoAdd = _pendingLocalChanges.Where(it => it.Value == Operation.PendDelete || it.Value == Operation.UndoAdd).Select(it => it.Key).ToList();
            if (itemsToUndoAdd.Any())
                WorkspaceUndoAdd(itemsToUndoAdd, GetPendingChangesCached());

            var itemsToAdd = _pendingLocalChanges.Where(it => it.Value == Operation.PendAdd).Select(it => it.Key).ToArray();
            if (itemsToAdd.Any())
                workspace.PendAdd(itemsToAdd);

            var itemsToDelete = _pendingLocalChanges.Where(it => it.Value == Operation.PendDelete).Select(it => it.Key).ToArray();
            if (itemsToDelete.Any())
                workspace.PendDelete(itemsToDelete);

            _pendingLocalChanges.Clear();

            return 0;
        }

        private void SetNotReadOnly(string filePath)
        {
            new FileInfo(filePath).IsReadOnly = false;
        }
    }

}
