extern crate protoc_rust;

fn main() {
    let protofiles = [
        "protofiles/clipboard.proto",
        "protofiles/request_master.proto",
        "protofiles/response_slave.proto",
    ];

    protoc_rust::Codegen::new()
        .out_dir("src/proto_rs")
        .inputs(protofiles)
        .include("protofiles")
        .run()
        .expect("Running protoc failed");
}
