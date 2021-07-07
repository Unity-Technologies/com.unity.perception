#!/bin/bash

usage() {
    echo "Usage: $0 [-e <local|real>] [-p <path string>] [-g <gcp project string>] [-r <region string>]" 1>&2; exit 1;
}

while getopts ":e:g:p:r:" o; do
    case "${o}" in
        e)
            environment=${OPTARG}
            ;;
        g)
            project=${OPTARG}
            ;;
        p)
            path=${OPTARG}
            ;;
        r)
            region=${OPTARG}
            ;;
        *)
            usage
            ;;
    esac
done
shift $((OPTIND-1))

if [[ -z "${environment}" ]] || [[ -z "${path}" ]] || [[ -z "${project}" ]]; then
    usage
fi

if [[ -z "${region}" ]]; then
    echo "No region provided. Defaulting to us-central1"
    region="us-central1"
fi

run() {
    echo "$ $@"
    "$@"
}

case "${environment}" in
    local)
        echo "> Running local..."
        run python main.py --source ${path} --eval-pct 20 --runner DirectRunner
        ;;
    real)
        NUM_WORKERS=${NUM_WORKERS:-4}
        MAX_NUM_WORKERS=${MAX_NUM_WORKERS:-64}
        echo "> Running on real data starting with ${NUM_WORKERS} workers and ${MAX_NUM_WORKERS} max workers..."
        run python main.py --setup_file=./setup.py \
         --source ${path} \
         --eval-pct 20 \
         --runner DataflowRunner \
         --project ${project} \
         --region ${region} \
         --temp_location ${path}/tmp \
         --staging_location  ${path}/stg \
         --num_workers ${NUM_WORKERS} \
         --max_num_workers ${MAX_NUM_WORKERS} \
         --autoscaling_algorithm THROUGHPUT_BASED
        ;;
    *)
        usage
        ;;
esac
