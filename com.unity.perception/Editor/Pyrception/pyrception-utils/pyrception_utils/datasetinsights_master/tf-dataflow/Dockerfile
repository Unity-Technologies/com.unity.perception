FROM python:3.7.9-buster
ENV DEBIAN_FRONTEND noninteractive

RUN curl -O https://dl.google.com/dl/cloudsdk/channels/rapid/downloads/google-cloud-sdk-320.0.0-linux-x86_64.tar.gz && \
    tar -xvf google-cloud-sdk-320.0.0-linux-x86_64.tar.gz && \
    ./google-cloud-sdk/install.sh -q && \
    echo "source /google-cloud-sdk/completion.bash.inc" >> /root/.bashrc && \
    echo "source /google-cloud-sdk/path.bash.inc" >> /root/.bashrc

RUN mkdir /tf-dataflow
ADD . /tf-dataflow
WORKDIR /tf-dataflow

RUN pip install -r requirements.txt

CMD ["bash"]
