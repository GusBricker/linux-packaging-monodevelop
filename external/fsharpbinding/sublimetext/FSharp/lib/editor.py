# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

from FSharp.fsac import server
from FSharp.fsac.client import FsacClient
from FSharp.fsac.request import CompilerLocationRequest
from FSharp.fsac.request import ProjectRequest
from FSharp.fsac.request import ParseRequest
from FSharp.sublime_plugin_lib import PluginLogger
from FSharp.lib.project import FSharpFile
from FSharp.lib.project import FSharpProjectFile


_logger = PluginLogger(__name__)


class Editor(object):
    """Global editor state.
    """
    def __init__(self, resp_proc):
        _logger.info ('starting fsac server...')
        self.fsac = FsacClient(server.start(), resp_proc)
        self.compilers_path = None
        self.project_file = None
        self.fsac.send_request (CompilerLocationRequest())

    @property
    def compiler_path(self):
        if self.compilers_path is None:
            return None
        return os.path.join(self.compilers_path, 'fsc.exe')

    @property
    def interpreter_path(self):
        if self.compilers_path is None:
            return None
        return os.path.join(self.compilers_path, 'fsi.exe')

    def refresh(self, fs_file):
        assert isinstance(fs_file, FSharpFile), 'wrong argument: %s' % fs_file
        # todo: run in alternate thread
        if not self.project_file:
            self.project_file = FSharpProjectFile.from_path(fs_file.path)
            return
        if not self.project_file.governs(fs_file.path):
            new_project_file = FSharpProjectFile.from_path(fs_file.path)
            self.project_file = new_project_file
        self.set_project()

    def set_project(self):
        self.fsac.send_request(ProjectRequest(self.project_file.path))

    def parse_file(self, fs_file, content):
        self.fsac.send_request(ParseRequest(fs_file.path, content))
