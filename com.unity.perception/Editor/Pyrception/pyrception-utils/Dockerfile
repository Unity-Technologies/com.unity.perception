FROM python:3.8-slim-buster
COPY . /pyrception-utils
WORKDIR /pyrception-utils
RUN pip install -e .
EXPOSE 8501
ENTRYPOINT pyrception-utils preview --data="."
