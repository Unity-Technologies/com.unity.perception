import argparse
import os

import streamlit.bootstrap

cli = argparse.ArgumentParser()
subparsers = cli.add_subparsers(dest="subcommand")


def argument(*name_or_flags, **kwargs):
    return ([*name_or_flags], kwargs)


def subcommand(args=[], parent=subparsers):
    def decorator(func):
        parser = parent.add_parser(func.__name__, description=func.__doc__)
        for arg in args:
            parser.add_argument(*arg[0], **arg[1])
        parser.set_defaults(func=func)

    return decorator


@subcommand(
    [argument("--data", type=str, help="The path to the main perception data folder.")]
)
def preview(args):
    """Previews the dataset in a streamlit app."""
    dirname = os.path.dirname(__file__)
    filename = os.path.join(dirname, "preview.py")
    if args.data is None:
        print("Data directory not specified!")
    else:
        args = [args.data]
        # _config.set_option("server.headless", True)
        streamlit.bootstrap.run(filename, "", args)


def main():
    args = cli.parse_args()
    if args.subcommand is None:
        cli.print_help()
    else:
        args.func(args)


if __name__ == "__main__":
    main()
