import os

import click


class Entrypoint(click.MultiCommand):
    """ Click MultiCommand Entrypoint For Datasetinsights CLI
    """

    def list_commands(self, ctx):
        """Dynamically get the list of commands."""
        rv = []
        for filename in os.listdir(os.path.dirname(__file__)):
            if filename.endswith(".py") and not filename.startswith("__init__"):
                rv.append(filename[:-3])
        rv.sort()

        return rv

    def get_command(self, ctx, name):
        """Dynamically get the command."""
        ns = {}
        fn = os.path.join(os.path.dirname(__file__), name + ".py")
        if not os.path.exists(fn):
            return None
        with open(fn) as f:
            code = compile(f.read(), fn, "exec")
            eval(code, ns, ns)

        return ns["cli"]
